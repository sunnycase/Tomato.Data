//
// DataImport
// 列眉
//
// 作者             SunnyCase
// 创建日期         2015-07-16
// 修改记录
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tomato.Data.DataImport
{
    /// <summary>
    /// 列眉
    /// </summary>
    public struct ColumnHeader
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public Type Type { get; set; }
    }
}
