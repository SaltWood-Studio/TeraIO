using System;
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

public class RsaStream : Stream
{
    protected readonly Stream _stream;
    protected RSA _rsaPrivate;
    protected RSA _rsaPublic;
    protected RSAParameters _publicKey;
    protected RSA? _remotePublicKey;
    protected ushort _protocolVersion = 1;

    public RsaStream(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!_stream.CanRead || !_stream.CanWrite)
        {
            throw new InvalidOperationException("Stream must be readable and writable.");
        }
        _rsaPrivate = RSA.Create();
        _rsaPublic = RSA.Create();
        _publicKey = _rsaPrivate.ExportParameters(false);
        _rsaPublic.ImportParameters(_publicKey);
    }

    public void Handshake()
    {
        // Send public key
        byte[] publicKeyBytes = _rsaPublic.ExportRSAPublicKey();
        _stream.Write(publicKeyBytes, 0, publicKeyBytes.Length);
        _stream.Flush();

        // Receive remote public key
        byte[] remotePublicKeyBytes = new byte[4096]; // Adjust size as needed
        int bytesRead = _stream.Read(remotePublicKeyBytes, 0, remotePublicKeyBytes.Length);
        if (bytesRead == 0) throw new IOException("Failed to read remote public key.");

        _remotePublicKey = RSA.Create();
        _remotePublicKey.ImportRSAPublicKey(remotePublicKeyBytes.AsSpan(0, bytesRead), out _);

        // Test encryption/decryption
        byte[] helloBytes = Encoding.UTF8.GetBytes("RSA HELLO");
        byte[] encryptedHello = _remotePublicKey.Encrypt(helloBytes, RSAEncryptionPadding.OaepSHA256);
        _stream.Write(encryptedHello, 0, encryptedHello.Length);
        _stream.Flush();

        byte[] responseBytes = new byte[4096]; // Adjust size as needed
        int responseLength = _stream.Read(responseBytes, 0, responseBytes.Length);
        if (responseLength == 0) throw new IOException("Failed to read response.");
        byte[] decryptedResponse = _rsaPrivate.Decrypt(responseBytes.AsSpan(0, responseLength), RSAEncryptionPadding.OaepSHA256);
        string decryptedResponseStr = Encoding.UTF8.GetString(decryptedResponse);
        if (decryptedResponseStr != "RSA HELLO") throw new InvalidOperationException("Handshake failed.");

        // Send protocol version
        byte[] versionBytes = BitConverter.GetBytes(_protocolVersion);
        _stream.Write(versionBytes, 0, versionBytes.Length);
        _stream.Flush();

        // Receive protocol version
        byte[] remoteVersionBytes = new byte[2];
        int versionLength = _stream.Read(remoteVersionBytes, 0, remoteVersionBytes.Length);
        if (versionLength == 0) throw new IOException("Failed to read remote protocol version.");
        ushort remoteVersion = BitConverter.ToUInt16(remoteVersionBytes);
        if (remoteVersion != _protocolVersion) throw new InvalidOperationException("Protocol version mismatch.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        // The max block size depends on the key size and padding. With OAEP and SHA-256, it's approximately: KeySize/8 - 2*HashSize - 2
        int maxBlockSize = _remotePublicKey!.KeySize / 8 - 2 * 32 - 2;

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


    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset and count must be non-negative.");
        if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset and count relative to buffer length.");

        using (var cryptoStream = new MemoryStream())
        {
            int totalBytesRead = 0;
            int encryptedBlockSize = _rsaPrivate.KeySize / 8; // Each encrypted block's size should be equal to the RSA key size in bytes

            byte[] encryptedBuffer = new byte[encryptedBlockSize];
            int bytesRead;

            while ((bytesRead = _stream.Read(encryptedBuffer, 0, encryptedBlockSize)) > 0)
            {
                if (bytesRead != encryptedBlockSize)
                    throw new CryptographicException("The length of the data to decrypt is not valid for the size of this key.");

                byte[] decryptedBlock = _rsaPrivate.Decrypt(encryptedBuffer, RSAEncryptionPadding.OaepSHA256);

                if (decryptedBlock.Length > 0)
                {
                    int copySize = Math.Min(decryptedBlock.Length, count - totalBytesRead);
                    Array.Copy(decryptedBlock, 0, buffer, offset + totalBytesRead, copySize);
                    totalBytesRead += copySize;
                }

                if (totalBytesRead >= count)
                    break;
            }

            return totalBytesRead;
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

    #endregion
}