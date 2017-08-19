﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TMTControls.TMTDataGrid;

namespace TMTControls
{
    public static class TMTExtend
    {
        public static IReadOnlyList<TMTDataGridView> GetChildDataGridViewList(this Control parentControl)
        {
            if (parentControl == null)
            {
                throw new ArgumentNullException(nameof(parentControl));
            }

            var childTableList = new List<TMTDataGridView>();

            foreach (Control childControl in parentControl.Controls)
            {
                if (childControl is TMTDataGridView tmtDataGridView)
                {
                    childTableList.Add(tmtDataGridView);
                }
                childTableList.AddRange(GetChildDataGridViewList(childControl));
            }

            return childTableList;
        }

        public static DataTable GetDataSourceTableChanges(this DataTable table, string tableName)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            table.Constraints.Clear();
            var changedData = table.GetChanges(DataRowState.Added | DataRowState.Modified | DataRowState.Deleted);

            if (changedData != null && changedData.Rows.Count > 0)
            {
                List<DataRow> removeList = new List<DataRow>();
                foreach (DataRow row in changedData.Rows)
                {
                    if (row.RowState != DataRowState.Deleted && row.ItemArray.All(i => i == null || string.IsNullOrWhiteSpace(i.ToString())))
                    {
                        removeList.Add(row);
                    }
                    if (row.RowState == DataRowState.Modified)
                    {
                        bool actualyModified = false;
                        foreach (DataColumn col in changedData.Columns)
                        {
                            if (row[col.ColumnName] != null && row[col.ColumnName, DataRowVersion.Original] == null)
                            {
                                actualyModified = true;
                                break;
                            }
                            else if (row[col.ColumnName] == null && row[col.ColumnName, DataRowVersion.Original] != null)
                            {
                                actualyModified = true;
                                break;
                            }
                            else if (row[col.ColumnName] is byte[])
                            {
                                if ((row[col.ColumnName, DataRowVersion.Original] as byte[]) == null)
                                {
                                    actualyModified = true;
                                    break;
                                }
                                if ((row[col.ColumnName] as byte[]).SequenceEqual<byte>(row[col.ColumnName, DataRowVersion.Original] as byte[]) == false)
                                {
                                    actualyModified = true;
                                    break;
                                }
                            }
                            else if (row[col.ColumnName].ToString() != row[col.ColumnName, DataRowVersion.Original].ToString())
                            {
                                actualyModified = true;
                                break;
                            }
                        }
                        if (actualyModified == false)
                        {
                            removeList.Add(row);
                        }
                    }
                }
                foreach (DataRow removeRow in removeList)
                {
                    changedData.Rows.Remove(removeRow);
                }

                changedData.TableName = tableName;
            }

            return changedData;
        }

        public static DataTable GetSearchConditionTable()
        {
            var searchConditionTable = new DataTable
            {
                TableName = "search_condition",
                Locale = CultureInfo.InvariantCulture
            };
            searchConditionTable.Columns.Add("COLUMN");
            searchConditionTable.Columns.Add("VALUE");
            searchConditionTable.Columns.Add("TYPE");
            searchConditionTable.Columns.Add("IS_FUNCTION");

            return searchConditionTable;
        }

        public static System.ServiceModel.EndpointAddress GetUrl()
        {
            if (Properties.Settings.Default.ServerURL.EndsWith("/", StringComparison.Ordinal) == false)
            {
                Properties.Settings.Default.ServerURL += "/";
            }
            return new System.ServiceModel.EndpointAddress(Properties.Settings.Default.ServerURL + Properties.Settings.Default.WebAPIName);
        }

        public static int? MaxValue(this DataRowCollection rows, string columnName)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            return rows.Cast<DataRow>().Where(r => r[columnName] != null && r[columnName] != DBNull.Value)
                                       .Select(r => r[columnName]).Cast<int>().Max();
        }

        public static string ValueString(this DataGridViewCell cell)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }
            return cell.Value?.ToString();
        }

        public static decimal? ValueDecimal(this DataGridViewCell cell)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }
            if (cell.Value != null)
            {
                if (decimal.TryParse(cell.Value.ToString(), out decimal decimalValue))
                {
                    return decimalValue;
                }
            }
            return null;
        }

        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static IReadOnlyCollection<DataGridViewRow> GetSelectedRowList(this DataGridView table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var rowList = new List<DataGridViewRow>();

            var selectedCellsRowIndexes = table.SelectedCells.Cast<DataGridViewCell>().Select(c => c.RowIndex);
            var selectedRowIndexes = new HashSet<int>(selectedCellsRowIndexes.Distinct());
            if (selectedRowIndexes.Count() > 0)
            {
                rowList = table.Rows.Cast<DataGridViewRow>().Where(r => selectedRowIndexes.Contains(r.Index)).ToList();
            }
            return rowList;
        }

        public static string IsSameRowTypeSelected(this DataGridView table, string uiColumnName)
        {
            string selectedType = string.Empty;

            var selectedRowList = table.GetSelectedRowList();
            if (selectedRowList.Count() > 0)
            {
                var statusGroups = selectedRowList.Select(r => r.Cells[uiColumnName].Value).GroupBy(s => s.ToString());
                if (statusGroups.Count() == 1)
                {
                    selectedType = statusGroups.First().Key;
                }
            }

            return selectedType;
        }
    }
}