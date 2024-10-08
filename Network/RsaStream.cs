﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;

namespace TeraIO.Network;

public class RsaStream : Stream, IDisposable
{
    protected readonly Stream _stream;
    private RSA _privateKey;
    public RSA PrivateKey
    {
        get => this._privateKey;
        set
        {
            this._privateKey = value;
            this.PublicKey = RSA.Create();
            this.PublicKey.ImportParameters(this.PrivateKey.ExportParameters(false));
        }
    }
    public RSA PublicKey { get; protected set; }
    protected RSAParameters _publicKey;
    protected RSA? _remotePublicKey;
    protected ushort _protocolVersion = 1;
    protected object _lock = new object();
    protected object _statusLock = new object();
    private RsaStreamStatus _status;
    private byte[] pendingBytes = Array.Empty<byte>();

    public int RsaKeyLength { get; protected set; }

    public RsaStreamStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                return _status;
            }
        }
        set
        {
            lock (_statusLock)
            {
                _status = value;
            }
        }
    }

    public RsaStream(Stream stream) : this(stream, 4096) { }

#pragma warning disable CS8618
    public RsaStream(Stream stream, int rsaKeyLength)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!_stream.CanRead || !_stream.CanWrite)
        {
            throw new InvalidOperationException("Stream must be readable and writable.");
        }
        this.RsaKeyLength = rsaKeyLength;
        this.PrivateKey = RSA.Create(rsaKeyLength);
    }
#pragma warning restore CS8618
    
    public void Handshake() => Handshake(null);

    public void Handshake(string? signature = null)
    {
        this.Status = RsaStreamStatus.Handshaking;
        // Send public key
        byte[] publicKeyBytes = PublicKey.ExportRSAPublicKey();
        _stream.Write(publicKeyBytes, 0, publicKeyBytes.Length);
        _stream.Flush();

        // Receive remote public key
        byte[] remotePublicKeyBytes = new byte[this.RsaKeyLength]; // Adjust size as needed
        int bytesRead = _stream.Read(remotePublicKeyBytes, 0, remotePublicKeyBytes.Length);
        if (bytesRead == 0) throw new IOException("Failed to read remote public key.");

        _remotePublicKey = RSA.Create();
        _remotePublicKey.ImportRSAPublicKey(remotePublicKeyBytes.AsSpan(0, bytesRead), out _);

        if (signature != null && GetRsaFingerprint(_remotePublicKey) != signature)
        {
            this.Status = RsaStreamStatus.AuthenticationFailed;
            throw new Exception($"Remote fingerprint doen't match: expected \"{signature}\", but \"{GetRsaFingerprint(_remotePublicKey)}\" found.");
        }

        // Test encryption/decryption
        byte[] helloBytes = Encoding.UTF8.GetBytes("RSA HELLO");
        byte[] encryptedHello = _remotePublicKey.Encrypt(helloBytes, RSAEncryptionPadding.OaepSHA256);
        _stream.Write(encryptedHello, 0, encryptedHello.Length);
        _stream.Flush();

        byte[] responseBytes = new byte[this.RsaKeyLength]; // Adjust size as needed
        int responseLength = _stream.Read(responseBytes, 0, responseBytes.Length);
        if (responseLength == 0) throw new IOException("Failed to read response.");
        byte[] decryptedResponse = PrivateKey.Decrypt(responseBytes.AsSpan(0, responseLength), RSAEncryptionPadding.OaepSHA256);
        string decryptedResponseStr = Encoding.UTF8.GetString(decryptedResponse);
        if (decryptedResponseStr != "RSA HELLO") throw new InvalidOperationException("Handshake failed.");

        // Send protocol version
        byte[] versionBytes = BitConverter.GetBytes(_protocolVersion);
        _stream.Write(versionBytes, 0, versionBytes.Length);
        _stream.Flush();

        // Receive protocol version
        byte[] remoteVersionBytes = new byte[2];
        int versionLength = _stream.Read(remoteVersionBytes, 0, remoteVersionBytes.Length);
        if (versionLength == 0)
        {
            this.Status = RsaStreamStatus.Failed;
            throw new IOException("Failed to read remote protocol version.");
        }
        ushort remoteVersion = BitConverter.ToUInt16(remoteVersionBytes);
        if (remoteVersion != _protocolVersion)
        {
            this.Status = RsaStreamStatus.ProtocolVersionMismatch;
            throw new InvalidOperationException("Protocol version mismatch.");
        }
        this.Status = RsaStreamStatus.Established;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        lock (_lock)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            // The max block size depends on the key size and padding. With OAEP and SHA-256, it's approximately: KeySize/8 - 2*HashSize - 2
            int maxBlockSize = _remotePublicKey!.KeySize / 8 - 2 * 32 - 2;

            long messageLength = count;
            _stream.Write(BitConverter.GetBytes(messageLength));

            using (var cryptoStream = new MemoryStream())
            {
                for (int i = offset; i < offset + count; i += maxBlockSize)
                {
                    int blockSize = Math.Min(maxBlockSize, count - (i - offset));
                    byte[] block = new byte[blockSize];
                    Array.Copy(buffer, i, block, 0, blockSize);

                    byte[] encryptedBlock = _remotePublicKey.Encrypt(block, RSAEncryptionPadding.OaepSHA256);
                    cryptoStream.Write(encryptedBlock, 0, encryptedBlock.Length);
                }

                _stream.Write(cryptoStream.ToArray(), 0, (int)cryptoStream.Length);
                _stream.Flush();
            }
        }
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        lock (_lock)
        {
            if (this.pendingBytes != null && this.pendingBytes.Length != 0)
            {
                int actualSize = Math.Min(this.pendingBytes.Length, count);
                (byte[] temp, this.pendingBytes) = (this.pendingBytes[0..actualSize], this.pendingBytes[actualSize..]);
                Array.Copy(temp, 0, buffer, offset, actualSize);
                return actualSize;
            }

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset and count must be non-negative.");
            if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset and count relative to buffer length.");

            using (var cryptoStream = new MemoryStream())
            {
                int totalBytesRead = 0;
                int encryptedBlockSize = PrivateKey.KeySize / 8; // Each encrypted block's size should be equal to the RSA key size in bytes

                byte[] encryptedBuffer = new byte[encryptedBlockSize];
                int bytesRead;

                byte[] decryptedBlock = Array.Empty<byte>();

                byte[] messageLengthByte = new byte[8];
                if (_stream.Read(messageLengthByte) < 8) throw new Exception("Unable to read message length.");
                long messageLength = BitConverter.ToInt64(messageLengthByte);
                byte[] restBytes = new byte[Math.Max(0, messageLength - count)];
                long actualSize = Math.Min(messageLength, count);
                int copySize = 0;

                while ((bytesRead = _stream.Read(encryptedBuffer, 0, encryptedBlockSize)) > 0)
                {
                    if (bytesRead != encryptedBlockSize)
                        throw new CryptographicException("The length of the data to decrypt is not valid for the size of this key.");

                    decryptedBlock = PrivateKey.Decrypt(encryptedBuffer, RSAEncryptionPadding.OaepSHA256);

                    if (decryptedBlock.Length > 0)
                    {
                        copySize = Math.Min(decryptedBlock.Length, count - totalBytesRead);
                        Array.Copy(decryptedBlock, 0, buffer, offset + totalBytesRead, copySize);
                        totalBytesRead += decryptedBlock.Length;
                    }

                    if (totalBytesRead >= actualSize)
                        break;
                }
                if (totalBytesRead > actualSize)
                {
                    Array.Copy(decryptedBlock[copySize..(int)(copySize + totalBytesRead - actualSize)], restBytes, (totalBytesRead - actualSize));
                }
                if (totalBytesRead < actualSize &&
                    restBytes.Length != 0 &&
                    _stream.Read(restBytes, 0, restBytes.Length) != restBytes.Length)
                    throw new Exception("Message length doesn't match.");
                this.pendingBytes = restBytes;

                return totalBytesRead;
            }
        }
    }

    #region Stream Overrides

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;
    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override void Flush() => _stream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
    public override void SetLength(long value) => _stream.SetLength(value);

    public override void Close()
    {
        _stream.Close();
        this.Status = RsaStreamStatus.Closed;
    }

    #endregion

    #region IDisposable Overrides
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.Close();
            _stream.Dispose();
            this.Status = RsaStreamStatus.Disposed;
        }
    }
    #endregion

    #region Static Methods
    public static string GetRsaFingerprint(RSA rsa) => $"SHA256:{Convert.ToBase64String(SHA256.HashData(rsa.ExportRSAPublicKey())).ToLower()}";
    #endregion
}