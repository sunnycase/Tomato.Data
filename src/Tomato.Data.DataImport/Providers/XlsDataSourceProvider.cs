//
// DataImport
// Xls (Excel) 数据源提供程序
//
// 作者             SunnyCase
// 创建日期         2015-07-16
// 修改记录
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Tomato.Data.DataImport.Infrastructure;
using System.Xml.Serialization;
using System.IO;
using System.Data;
using System.Diagnostics;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Tomato.Data.DataImport.Providers
{
    /// <summary>
    /// Xls (Excel) 数据源提供程序接口
    /// </summary>
    public sealed class XlsDataSourceProvider<T> : DataSourceProviderBase<T> where T : class
    {
        private readonly HSSFWorkbook workbook;

        /// <summary>
        /// 获取或设置要读取的表索引
        /// </summary>
        public int SheetIndex { get; set; }

        private string[] sheetNames;
        /// <summary>
        /// 获取表名称
        /// </summary>
        public string[] SheetNames
        {
            get
            {
                if (sheetNames == null)
                {
                    sheetNames = (from idx in Enumerable.Range(0, workbook.NumberOfSheets)
                                  select workbook.GetSheetName(idx)).ToArray();
                }
                return sheetNames;
            }
        }

        public XlsDataSourceProvider(Stream stream)
        {
            workbook = new HSSFWorkbook(stream);
        }

        public override IEnumerable<IRow<T>> ReadAll()
        {
            var sheet = workbook.GetSheetAt(SheetIndex);

            if (HasHeaders)
            {
                var columnIndexes = Headers.Select(o => o.ColumnIndex).ToArray();
                foreach (IRow row in sheet.GetRows().SkipWhile(o => o.RowNum <= HeadersRowIndex))
                {
                    var values = columnIndexes.Select(i => row.GetCell(i)).Select(c => c == null ? null : c.GetValue());
                    if (values.Any(o => o != null))
                        yield return MakeModel(values, row.RowNum);
                }
            }
            else
            {
                foreach (IRow row in sheet.GetRows())
                {
                    var values = row.Cells.Select(o => o.GetValue());
                    if (values.Any(o => o != null))
                        yield return MakeModel(values, row.RowNum);
                }
            }
        }

        public override IRow<T> ReadRow(int rowIndex)
        {
            var sheet = workbook.GetSheetAt(SheetIndex);
            var row = sheet.GetRow(rowIndex);

            var values = row?.Cells.Select(o => o.GetValue());
            if (values?.Any(o => o != null) == true)
                return MakeModel(values, row.RowNum);
            return null;
        }

        private ColumnHeader[] headers;
        public override ColumnHeader[] Headers
        {
            get
            {
                if (headers == null && HasHeaders)
                {
                    var row = workbook.GetSheetAt(SheetIndex).GetRow(HeadersRowIndex);
                    headers = (from c in row.Cells
                               select new ColumnHeader
                               {
                                   Name = (c.GetValue() ?? string.Empty).ToString(),
                                   Type = typeof(object),
                                   ColumnIndex = c.ColumnIndex
                               }).ToArray();
                }
                return headers;
            }
        }
    }
}
