//
// DataImport
// 数据源提供程序接口
//
// 作者             SunnyCase
// 创建日期         2015-07-16
// 修改记录
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tomato.Data.DataImport.Infrastructure
{
    public interface IRow<out T>
    {
        int Index { get; }
        T Data { get; }
    }

    struct Row<T> : IRow<T>
    {
        public int Index { get; set; }
        public T Data { get; set; }
    }

    /// <summary>
    /// 数据源提供程序接口。
    /// </summary>
    /// <typeparam name="T">数据项模型。</typeparam>
    /// <remarks>此接口的实现从TXT，XLS等数据源中提取数据。</remarks>
    public interface IDataSourceProvider<T> where T : class
    {
        /// <summary>
        /// 获取或设置是否有表头。
        /// </summary>
        bool HasHeaders { get; set; }

        /// <summary>
        /// 获取或设置表头的行号。
        /// </summary>
        int HeadersRowIndex { get; set; }

        /// <summary>
        /// 如果有表头则获取表头，否则返回 null。
        /// </summary>
        ColumnHeader[] Headers { get; }

        /// <summary>
        /// 读取所有数据项。
        /// </summary>
        IEnumerable<IRow<T>> ReadAll();
    }
}
