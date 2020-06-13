﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NewLife.Log
{
    /// <summary>跟踪片段构建器</summary>
    public interface ISpanBuilder
    {
        #region 属性
        /// <summary>跟踪器</summary>
        ITracer Tracer { get; }

        /// <summary>操作名</summary>
        String Name { get; set; }

        /// <summary>采样总数</summary>
        Int32 Total { get; }

        /// <summary>错误次数</summary>
        Int32 Errors { get; }

        /// <summary>总耗时。单位ms</summary>
        Int64 Cost { get; }

        /// <summary>最大耗时。单位ms</summary>
        Int32 MaxCost { get; }

        /// <summary>正常采样</summary>
        IList<ISpan> Samples { get; }

        /// <summary>异常采样</summary>
        IList<ISpan> ErrorSamples { get; }
        #endregion

        #region 方法
        /// <summary>开始一个Span</summary>
        /// <returns></returns>
        ISpan Start();

        /// <summary>完成Span</summary>
        /// <param name="span"></param>
        void Finish(ISpan span);
        #endregion
    }

    /// <summary>跟踪片段构建器</summary>
    public class DefaultSpanBuilder : ISpanBuilder
    {
        #region 属性
        /// <summary>跟踪器</summary>
        public ITracer Tracer { get; }

        /// <summary>操作名</summary>
        public String Name { get; set; }

        private Int32 _Total;
        /// <summary>采样总数</summary>
        public Int32 Total => _Total;

        private Int32 _Errors;
        /// <summary>错误次数</summary>
        public Int32 Errors => _Errors;

        private Int64 _Cost;
        /// <summary>总耗时。单位ms</summary>
        public Int64 Cost => _Cost;

        /// <summary>最大耗时。单位ms</summary>
        public Int32 MaxCost { get; private set; }

        /// <summary>正常采样</summary>
        public IList<ISpan> Samples { get; } = new List<ISpan>();

        /// <summary>异常采样</summary>
        public IList<ISpan> ErrorSamples { get; } = new List<ISpan>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="tracer"></param>
        /// <param name="name"></param>
        public DefaultSpanBuilder(ITracer tracer, String name)
        {
            Tracer = tracer;
            Name = name;
        }
        #endregion

        #region 方法
        /// <summary>开始一个Span</summary>
        /// <returns></returns>
        public virtual ISpan Start()
        {
            var span = new DefaultSpan(this);
            span.SetTracerId();

            return span;
        }

        /// <summary>完成Span</summary>
        /// <param name="span"></param>
        public virtual void Finish(ISpan span)
        {
            var total = Interlocked.Increment(ref _Total);
            //if (span.Error != null) Interlocked.Increment(ref _Errors);
            Interlocked.Add(ref _Cost, span.Cost);

            if (MaxCost < span.Cost) MaxCost = span.Cost;

            // 处理采样
            if (span.Error != null)
            {
                if (Interlocked.Increment(ref _Errors) <= Tracer.MaxErrors)
                {
                    lock (ErrorSamples)
                    {
                        ErrorSamples.Add(span);
                    }
                }
            }
            else
            {
                if (total <= Tracer.MaxSamples)
                {
                    lock (Samples)
                    {
                        Samples.Add(span);
                    }
                }
            }
        }
        #endregion
    }
}