﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace TMTControls.TMTDialogs
{
    public partial class TMTListOfValueDialog : TMTDialog
    {
        private TMTSearchDialog searchDialog;

        public Dictionary<string, object> SelectedRow { get; private set; }

        public TMTListOfValueDialog()
        {
            InitializeComponent();
        }

        public string HeaderLabel
        {
            get
            {
                return labelHeader.Text;
            }
            set
            {
                labelHeader.Text = value;
            }
        }

        public void SetDataSourceTable(DataTable table)
        {
            tmtDataGridViewMain.SetTheme();

            tmtDataGridViewMain.DataSourceTable = table;
            if (tmtDataGridViewMain.DataSourceTable != null)
            {
                var colType = this.GetColumnTypeDictionary();

                this.SelectedRow = new Dictionary<string, object>();

                tmtDataGridViewMain.Columns.Clear();

                if (searchDialog == null)
                {
                    searchDialog = new TMTSearchDialog();
                }
                searchDialog.EntityList.Clear();

                DataGridViewColumn vCol;
                foreach (DataColumn dCol in tmtDataGridViewMain.DataSourceTable.Columns)
                {
                    if (colType[dCol.ColumnName] == typeof(byte[]).FullName)
                    {
                        vCol = new DataGridViewImageColumn();
                    }
                    else if (colType[dCol.ColumnName] == "ENUM_BOOLEAN")
                    {
                        vCol = new DataGridViewCheckBoxColumn();
                        var checkVCol = (vCol as DataGridViewCheckBoxColumn);
                        checkVCol.FalseValue = "FALSE";
                        checkVCol.TrueValue = "TRUE";
                        checkVCol.IndeterminateValue = "FALSE";
                    }
                    else
                    {
                        vCol = new DataGridViewTextBoxColumn();
                    }
                    vCol.Name = "col" + dCol.ColumnName;
                    vCol.DataPropertyName = dCol.ColumnName;
                    vCol.HeaderText = GetHeaderText(dCol.Caption);
                    if (dCol.ColumnName == "HIGHLIGHT_COLOR")
                    {
                        vCol.Visible = false;
                    }

                    this.SelectedRow.Add(dCol.Caption, string.Empty);

                    tmtDataGridViewMain.Columns.Add(vCol);
                }
                tmtDataGridViewMain.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

                buttonSearch.Enabled = (tmtDataGridViewMain.DataSourceTable.Rows.Count > 1);
            }
        }

        private Dictionary<string, string> GetColumnTypeDictionary()
        {
            var colType = new Dictionary<string, string>();

            var sourceTable = tmtDataGridViewMain.DataSourceTable;

            var enumBoolean = new List<string> { "TRUE", "FALSE" };

            foreach (DataColumn dCol in sourceTable.Columns)
            {
                colType.Add(dCol.ColumnName, dCol.DataType.FullName);

                if (typeof(string).FullName == dCol.DataType.FullName)
                {
                    var distinctValueList = sourceTable.Rows.Cast<DataRow>().Select(r => r[dCol.ColumnName]).Where(i => i.GetType() != typeof(DBNull)).Cast<string>().Distinct().ToList();
                    if (distinctValueList.Intersect(enumBoolean).Any())
                    {
                        colType[dCol.ColumnName] = "ENUM_BOOLEAN";
                    }
                }
            }

            return colType;
        }

        private static string GetHeaderText(string orginalText)
        {
            orginalText = orginalText.Replace("_", " ").ToLowerInvariant().Trim();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(orginalText);
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (tmtDataGridViewMain.SelectedRows.Count == 1)
                {
                    var row = tmtDataGridViewMain.SelectedRows[0];

                    var keyList = this.SelectedRow.Keys.ToList();

                    foreach (string key in keyList)
                    {
                        this.SelectedRow[key] = row.Cells["col" + key].Value;
                    }

                    this.DialogResult = DialogResult.OK;
                }
            }
            catch
            {
                throw;
            }
        }

        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            try
            {
                if (searchDialog.EntityList == null || searchDialog.EntityList.Count == 0)
                {
                    foreach (DataGridViewColumn vCol in tmtDataGridViewMain.Columns)
                    {
                        var searchEntity = new SearchEntity()
                        {
                            Caption = vCol.HeaderText,
                            ColumnName = vCol.DataPropertyName,
                            DataType = tmtDataGridViewMain.DataSourceTable.Columns[vCol.DataPropertyName].DataType
                        };
                        if (vCol is DataGridViewCheckBoxColumn checkVCol)
                        {
                            searchEntity.IsCheckBox = true;
                            searchEntity.FalseValue = checkVCol.FalseValue;
                            searchEntity.TrueValue = checkVCol.TrueValue;
                            searchEntity.IndeterminateValue = checkVCol.IndeterminateValue;
                        }
                        searchDialog.EntityList.Add(searchEntity);
                    }
                }

                if (searchDialog.ShowDialog(this) == DialogResult.OK)
                {
                    string filter = string.Empty;

                    foreach (var sEntity in searchDialog.EntityList)
                    {
                        if (string.IsNullOrWhiteSpace(sEntity.Value) == false)
                        {
                            var sValueArray = sEntity.Value.ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            if (sValueArray.Length > 0)
                            {
                                for (int i = 0; i < sValueArray.Length; i++)
                                {
                                    if (string.IsNullOrWhiteSpace(filter) == false)
                                    {
                                        filter += " AND ";
                                    }

                                    var sValue = sValueArray[i].Trim();
                                    var operatorSymbol = GetOperator(sValue);
                                    sValue = sValue.Replace(operatorSymbol, string.Empty).Trim();

                                    filter += $" `{sEntity.ColumnName}` {operatorSymbol} '{sValue}' ";
                                }
                            }
                        }
                    }

                    tmtDataGridViewMain.DataSourceTable.DefaultView.RowFilter = filter;
                }
            }
            catch (Exception ex)
            {
                TMTErrorDialog.Show(this, ex, Properties.Resources.ERROR_FilteringLovValues);
            }
        }

        private static string GetOperator(string sValue)
        {
            string operatorSymbol = "=";
            if (sValue.StartsWith("<>", StringComparison.Ordinal))
            {
                operatorSymbol = "!=";
            }
            else if (sValue.StartsWith("!=", StringComparison.Ordinal))
            {
                operatorSymbol = "!=";
            }
            else if (sValue.StartsWith("<=", StringComparison.Ordinal))
            {
                operatorSymbol = "<=";
            }
            else if (sValue.StartsWith(">=", StringComparison.Ordinal))
            {
                operatorSymbol = ">=";
            }
            else if (sValue.StartsWith("<", StringComparison.Ordinal))
            {
                operatorSymbol = "<";
            }
            else if (sValue.StartsWith(">", StringComparison.Ordinal))
            {
                operatorSymbol = ">";
            }
            else if (sValue.Contains("%"))
            {
                operatorSymbol = "LIKE";
            }
            return operatorSymbol;
        }

        private void TmtDataGridViewMain_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    buttonOK.PerformClick();

                    e.Handled = true;
                }
            }
            catch
            {
            }
        }

        private void TmtDataGridViewMain_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left &&
                    e.Clicks == 2)
                {
                    buttonOK.PerformClick();
                }
            }
            catch
            {
            }
        }

        private void TmtDataGridViewMain_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                buttonOK.PerformClick();
            }
            catch
            {
            }
        }

        private void TmtDataGridViewMain_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (tmtDataGridViewMain.DataSourceTable.Columns.Contains("HIGHLIGHT_COLOR"))
                {
                    foreach (DataGridViewRow vRow in tmtDataGridViewMain.Rows)
                    {
                        var oColor = vRow.Cells["colHIGHLIGHT_COLOR"].ValueString();
                        if (string.IsNullOrWhiteSpace(oColor) == false)
                        {
                            vRow.DefaultCellStyle.BackColor = Color.FromName(oColor);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}