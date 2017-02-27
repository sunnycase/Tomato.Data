using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NPOI.HSSF.UserModel;

namespace Tomato.Data.DataImport.Providers
{
    public class XlsExportProvider
    {
        private HSSFWorkbook _workbook = new HSSFWorkbook();

        public XlsExportProvider()
        {

        }

        public void CreateSheet()
        {
            _workbook.CreateSheet();
        }

        public void CreateSheet(string name)
        {
            _workbook.CreateSheet(name);
        }

        public void FillData(int sheetIndex, IEnumerable<object[]> data)
        {
            var sheet = _workbook.GetSheetAt(sheetIndex);
            int rowId = 0;
            foreach (var item in data)
            {
                int columnId = 0;
                var row = sheet.CreateRow(rowId++);
                foreach (var field in item)
                {
                    var cell = row.CreateCell(columnId++);
                    cell.SetValue(field);
                }
            }
        }

        public void Save(Stream stream)
        {
            _workbook.Write(stream);
        }
    }
}
