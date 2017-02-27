//
// DataImport
// 数据源提供程序基类
//
// 作者             SunnyCase
// 创建日期         2015-07-16
// 修改记录
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Tomato.Data.DataImport.Infrastructure
{
    /// <summary>
    /// 数据源提供程序基类
    /// </summary>
    public abstract class DataSourceProviderBase<T> : IDisposable, IDataSourceProvider<T> where T : class
    {
        /// <summary>
        /// 列信息
        /// </summary>
        protected class ColumnInfo
        {
            private MethodInfo setter;
            private Type type;

            public ColumnInfo(MethodInfo setter, Type type)
            {
                this.setter = setter;
                this.type = type;
            }

            /// <summary>
            /// 设置列的值
            /// </summary>
            /// <param name="obj">模型对象</param>
            /// <param name="value">列的值</param>
            public void SetValue(T obj, object value)
            {
                setter.Invoke(obj, new[] { Convert.ChangeType(value, type) });
            }
        }

        private static readonly ReadOnlyCollection<ColumnInfo> columns;
        private static readonly bool isDynamicModel = false;

        /// <summary>
        /// 所有列的信息
        /// </summary>
        protected static ReadOnlyCollection<ColumnInfo> Columns { get { return columns; } }

        /// <summary>
        /// 获取是否动态模型
        /// </summary>
        protected static bool IsDynamicModel { get { return isDynamicModel; } }

        public bool HasHeaders { get; set; }
        public int HeadersRowIndex { get; set; }

        static DataSourceProviderBase()
        {
            // 如果是动态模型则不用分析
            if (typeof(T) == typeof(object))
                isDynamicModel = true;
            else
            {
                columns = BuildColumns();
            }
        }

        static ReadOnlyCollection<ColumnInfo> BuildColumns()
        {
            // 获取所有共有的未定义 XmlIgnore 特性的属性
            return new ReadOnlyCollection<ColumnInfo>(
                (from p in typeof(T).GetProperties()
                 where !p.IsDefined(typeof(XmlIgnoreAttribute), true)
                 select new ColumnInfo(p.GetSetMethod(), p.PropertyType)).ToList());
        }

        #region Dispose

        /// <summary>
        /// 是否资源已被释放
        /// </summary>
        public bool Disposed { get; protected set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DataSourceProviderBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }

        #endregion


        public abstract ColumnHeader[] Headers { get; }

        public abstract IEnumerable<IRow<T>> ReadAll();
        public abstract IRow<T> ReadRow(int rowIndex);

        protected IRow<T> MakeModel(IEnumerable<object> values, int rowIndex)
        {
            if (IsDynamicModel)
                return new Row<T>
                {
                    Index = rowIndex,
                    Data = (dynamic)values.ToArray()
                };
            else
                return new Row<T>
                {
                    Index = rowIndex,
                    Data = default(T)
                };
        }
    }
}
