﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeraIO.Runnable
{
    public abstract class RunnableBase : IRunnable
    {
        protected RunnableBase(string[] args, ILoggerBuilder? logbuilder = null)
        {
            Type type = GetType();
            if (logbuilder is not null)
                _logger = logbuilder.GetLogger(type);

            this.args = args;

            IsRunning = false;
            ThreadName = type.FullName ?? type.Name;
            _stopSemaphore = new(0);
            _stopTask = GetStopTask();

            Started += OnStarted;
            Stopped += OnStopped;
            ThrowException += OnThrowException;
        }

        protected RunnableBase(ILoggerBuilder? logbuilder = null) : this(new string[0], logbuilder)
        {

        }

        private readonly object _lock = new();

        private readonly ILogger? _logger;

        private readonly SemaphoreSlim _stopSemaphore;

        private Task _stopTask;

        public virtual Thread? Thread { get; protected set; }

        public virtual string ThreadName { get; protected set; }

        private string[] args;

        public virtual bool IsRunning { get; protected set; }

        public event EventHandler<IRunnable, EventArgs> Started;

        public event EventHandler<IRunnable, EventArgs> Stopped;

        public event EventHandler<IRunnable, ExceptionEventArgs> ThrowException;

        protected virtual void OnStarted(IRunnable sender, EventArgs e)
        {
            _logger?.Info($"线程({Thread?.Name ?? "null"})已启动");
        }

        protected virtual void OnStopped(IRunnable sender, EventArgs e)
        {
            _logger?.Info($"线程({Thread?.Name ?? "null"})已停止");
        }

        protected virtual void OnThrowException(IRunnable sender, ExceptionEventArgs e)
        {
            _logger?.Error($"线程({Thread?.Name ?? "null"})抛出了异常", e.Exception);
        }

        protected abstract int Run(string[] args);

        public virtual bool Start(string threadName)
        {
            ArgumentException.ThrowIfNullOrEmpty(threadName, nameof(threadName));

            ThreadName = threadName;
            return Start();
        }

        public virtual bool Start()
        {
            lock (_lock)
            {
                if (IsRunning)
                    return false;

                IsRunning = true;
                Thread = new(ThreadStart);
                Thread.Name = ThreadName;
                Thread.Start();
                return true;
            }
        }

        public virtual void Stop()
        {
            lock (_lock)
            {
                if (IsRunning)
                {
                    IsRunning = false;
                    int i = 0;
                    try
                    {
                        while (Thread is not null)
                        {
                            Thread.Join(1000);
                            if (!Thread.IsAlive)
                                break;
                            i++;
                            _logger?.Warn($"正在等待线程({Thread?.Name})停止,已等待{i}秒");
                            if (i >= 5)
                            {
                                _logger?.Warn($"即将强行停止线程({Thread?.Name})");
                                _stopSemaphore.Release();
                                _stopTask = GetStopTask();
                                Thread.Abort();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Thread is not null && Thread.IsAlive)
                            _logger?.Error($"无法停止线程({Thread?.Name})", ex);
                    }
                }
            }
        }

        public void WaitForStop()
        {
            _stopTask.Wait();
        }

        public async Task WaitForStopAsync()
        {
            await _stopTask;
        }

        protected async Task GetStopTask()
        {
            await _stopSemaphore.WaitAsync();
        }

        protected void ThreadStart()
        {
            try
            {
                Started.Invoke(this, EventArgs.Empty);
                Run(this.args);
            }
            catch (Exception ex)
            {
                ThrowException.Invoke(this, new(ex));
            }
            finally
            {
                IsRunning = false;
                _stopSemaphore.Release();
                _stopTask = GetStopTask();
                Stopped.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
