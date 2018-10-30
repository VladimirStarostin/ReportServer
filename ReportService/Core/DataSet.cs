﻿using System.Collections.Generic;
using ReportService.Interfaces.Protobuf;

namespace ReportService.Core
{
    public class DataSet
    {
        public  DataSetParameters dataSetParameters;

        public List<object[]> Rows = new List<object[]>();
    }

    public partial class ElementarySerializer
    {
        public DataSetParameters SaveHeaderFromClassFields<T>() where T : class
        {
            var innerFields = typeof(T).GetFields();

            var descriptor = new DataSetParameters();

            for (int i = 0; i < innerFields.Length; i++)
            {
                var field = innerFields[i];
                descriptor.Fields.Add(i + 1,
                    new ColumnInfo(field.Name, field.FieldType));
            }

            return descriptor;
        }
    }

    public class DataSetRow
    {
        public List<DataSetCell> Cells;
    }

    public class DataSetCell
    {

    }
}