using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace test
{
  public partial class MainForm : Form
  {
    private const string ConnStr = "Server=localhost;Database=test-db;user=root;password= ";
    private string _currentTable = "employees";

    private MySqlDataAdapter _da;
    private DataTable _tbl;

    public MainForm()
    {
      InitializeComponent();

      // события формы
      Load += MainForm_Load;
      FormClosing += MainForm_FormClosing;

      // UI-элементы
      btnReload.Click += (s, e) => LoadData();
      cmbTables.SelectedIndexChanged += cmbTables_SelectedIndexChanged;

      // работа с гридом
      dataGridView1.CellBeginEdit += DataGridView1_CellBeginEdit;
      dataGridView1.CellValidating += DataGridView1_CellValidating;
      dataGridView1.CellFormatting += DataGridView1_CellFormatting;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
      cmbTables.SelectedIndex = 0;
      LoadData();
    }
    
    private void LoadData()
    {
      try
      {
        if (dataGridView1.IsCurrentCellInEditMode) dataGridView1.CancelEdit();

        dataGridView1.DataSource = null;

        _da = new MySqlDataAdapter($"SELECT * FROM `{_currentTable}`", ConnStr);
        new MySqlCommandBuilder(_da);

        _tbl = new DataTable();
        _da.Fill(_tbl);
        dataGridView1.DataSource = _tbl;

        if (_tbl.Columns.Contains("id"))
          dataGridView1.Columns["id"].ReadOnly = true;

        foreach (DataGridViewColumn c in dataGridView1.Columns)
          if (c.Name.ToLower().EndsWith("_date"))
            c.DefaultCellStyle.Format = "yyyy.MM.dd";
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Не удалось открыть таблицу {_currentTable}:\n{ex.Message}", "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (_tbl == null) return;
      try
      {
        _da.Update(_tbl);
      }
      catch (Exception ex)  
      {
        var ans = MessageBox.Show($"Не удалось сохранить изменения:\n{ex.Message}\n\n" + "Выйти без сохранения?", "Ошибка соединения", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (ans == DialogResult.No) e.Cancel = true;
      }
    }
    private string _oldValue;
    private void DataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
    {
      _oldValue = dataGridView1[e.ColumnIndex, e.RowIndex].Value?.ToString();
    }
    private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
      var col = dataGridView1.Columns[e.ColumnIndex].Name.ToLower();
      if (col.EndsWith("_date") && e.Value is DateTime dt)
      {
        e.Value = dt.ToString("yyyy.MM.dd");
        e.FormattingApplied = true;
      }
    }

    private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
      var col = dataGridView1.Columns[e.ColumnIndex].Name.ToLower();
            var val = e.FormattedValue?.ToString();

            bool bad = false; string msg = "";

            if (col.EndsWith("_date") &&
                !DateTime.TryParseExact(val, "yyyy.MM.dd", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out _))
            {
                bad = true; msg = "Дата должна быть в формате YYYY.MM.DD.";
            }
            else if (col == "salary" &&
                     (!decimal.TryParse(val, out var sVal) || sVal < 0))
            {
                bad = true; msg = "Зарплата должна быть ≥ 0.";
            }

            if (!bad) return;

            var choice = MessageBox.Show(msg + "\n\nПовтор — правка, Отмена — откат.",
                                         "Неверный ввод",
                                         MessageBoxButtons.RetryCancel,
                                         MessageBoxIcon.Warning);

            if (choice == DialogResult.Cancel)
            {
                // вернуть старое, выйти из ячейки
                dataGridView1.CancelEdit();
                e.Cancel = false;
            }
            else
            {
                // остаться в ячейке
                e.Cancel = true;
            }
    }

    private void btnReload_Click(object sender, EventArgs e)
    {
      LoadData();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      _da.Update(_tbl);
    }
    private void cmbTables_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (cmbTables.SelectedItem == null) return;
      _currentTable = cmbTables.SelectedItem.ToString();
      LoadData();
    }

    private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {

    }
  }
}
