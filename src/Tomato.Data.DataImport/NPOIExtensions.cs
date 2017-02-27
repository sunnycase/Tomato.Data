using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;

namespace Tomato.Data.DataImport
{
    static class NPOIExtensions
    {
        public static IEnumerable<IRow> GetRows(this ISheet sheet)
        {
            var enu = sheet.GetRowEnumerator();

            while (enu.MoveNext())
                yield return (IRow)enu.Current;
        }

        public static object GetValue(this ICell cell)
        {
            var realValue = cell.GetValueCore();
            // 是合并的单元格则寻找到合并的单元格中第一个值
            if (cell.IsMergedCell)
                return cell.GetMergedRangeFirstCell().GetValueCore();
            return realValue;
        }

        private static object GetValueCore(this ICell cell)
        {
            var type = cell.CellType;
            if (type == CellType.Formula)
                type = cell.CachedFormulaResultType;

            var value = GetValueCore(cell, type);
            // 保留两位小数
            {
                var dValue = value as double?;
                if (dValue.HasValue)
                    return Math.Round(dValue.Value, 2);
            }
            return value;
        }

        private static object GetValueCore(ICell cell, CellType type)
        {
            switch (type)
            {
                case CellType.Blank:
                    return null;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Error:
                    return null;
                case CellType.Formula:
                    return null;
                case CellType.Numeric:
                    return IsCellDateFormatted(cell) ? (object)cell.DateCellValue : cell.NumericCellValue;
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Unknown:
                    return null;
            }
            return null;
        }

        private static readonly HashSet<short> _dateDataFormats = new HashSet<short>
        {
            164, 57
        };

        private static bool IsCellDateFormatted(ICell cell)
        {
            var dataFormat = cell.CellStyle.DataFormat;
            if (_dateDataFormats.Contains(dataFormat))
                return true;
            return DateUtil.IsCellDateFormatted(cell);
        }

        private static ICell GetMergedRangeFirstCell(this ICell cell)
        {
            if (!cell.IsMergedCell)
                throw new InvalidOperationException();
            var sheet = cell.Sheet;
            var range = (from i in Enumerable.Range(0, sheet.NumMergedRegions)
                         select sheet.GetMergedRegion(i)).First(o => o.IsInRange(cell.RowIndex, cell.ColumnIndex));
            return sheet.GetRow(range.FirstRow).GetCell(range.FirstColumn);
        }

        public static void SetValue(this ICell cell, object value)
        {
            if (value == null)
                cell.SetCellType(CellType.Blank);
            else
            {
                if (Nullable.GetUnderlyingType(value.GetType()) != null)
                    value = (value as dynamic).Value;

                if (value is bool)
                    cell.SetCellValue((bool)value);
                else if (value is string)
                    cell.SetCellValue((string)value);
                else if (value is DateTime)
                {
                    if (DateUtil.GetExcelDate((DateTime)value) >= 0.1)
                    {
                        cell.SetCellValue((DateTime)value);
                        var format = cell.Sheet.Workbook.CreateDataFormat();
                        var n = format.GetFormat("yyyy/MM/dd");
                        cell.CellStyle = FindOrCreateStyle(cell.Sheet.Workbook, n);
                    }
                }
                else if (value.IsNumber())
                    cell.SetCellValue(Convert.ToDouble(value));
                else
                    cell.SetCellValue(value.ToString());
            }
        }

        public static ICellStyle FindOrCreateStyle(IWorkbook workbook, short dataFormat)
        {
            for (short i = 0; i < workbook.NumCellStyles; i++)
            {
                var style = workbook.GetCellStyleAt(i);
                if (style.DataFormat == dataFormat)
                    return style;
            }
            var newStyle = workbook.CreateCellStyle();
            newStyle.DataFormat = dataFormat;
            return newStyle;
        }

        public static bool IsNumber(this object value)
        {
            if (value == null) return false;

            return value is sbyte
            || value is byte
            || value is short
            || value is ushort
            || value is int
            || value is uint
            || value is long
            || value is ulong
            || value is float
            || value is double
            || value is decimal;
        }
    }
}
