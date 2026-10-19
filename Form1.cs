using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Lab4_XML_Database
{
    public partial class Form1 : Form
    {
        string connString = "Data Source=meal.db;";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData("SELECT * FROM Product");
        }

        private void LoadData(string query)
        {
            try
            {
                using (var conn = new SQLiteConnection(connString))
                {
                    var adapter = new SQLiteDataAdapter(query, conn);
                    var ds = new DataSet();
                    adapter.Fill(ds, "Product");
                    dataGridView1.DataSource = ds.Tables["Product"];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStep1_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = new SQLiteConnection(connString))
                {
                    var adapter = new SQLiteDataAdapter("SELECT * FROM Product", conn);
                    var ds = new DataSet("Products");
                    adapter.Fill(ds, "Product");
                    ds.WriteXml("Product.xml");
                    dataGridView1.DataSource = ds.Tables["Product"];
                    MessageBox.Show("Файл Product.xml успішно створено!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStep2_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = new SQLiteConnection(connString))
                {
                    var adapter = new SQLiteDataAdapter("SELECT * FROM Product WHERE Product_Price > 1000", conn);
                    var ds = new DataSet("SelectedProducts");
                    adapter.Fill(ds, "Product");
                    ds.WriteXml("SelectedProducts.xml");
                    dataGridView1.DataSource = ds.Tables["Product"];
                    MessageBox.Show("Файл SelectedProducts.xml створено!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStep3_Click(object sender, EventArgs e)
        {
            if (!File.Exists("Product.xml"))
            {
                MessageBox.Show("Спочатку створіть Product.xml (Крок 1)");
                return;
            }

            var ds = new DataSet();
            using (XmlReader reader = XmlReader.Create("Product.xml"))
            {
                ds.ReadXml(reader);
            }

            DataView dv = ds.Tables[0].DefaultView;
            dv.Sort = "Product_Name";
            dataGridView1.DataSource = dv;

            int index1 = dv.Find("Product2");
            if (index1 != -1)
            {
                DataRowView row = dv[index1];
                MessageBox.Show($"Знайдено продукт: {row["Product_Name"]}, Ціна: {row["Product_Price"]}", "Успішний пошук");
            }
            else
            {
                MessageBox.Show("Елемент Product2 не знайдено");
            }

            int index2 = dv.Find("NonExistentProduct");
            if (index2 != -1)
            {
                MessageBox.Show($"Знайдено: {dv[index2]["Product_Name"]}");
            }
            else
            {
                MessageBox.Show($"Елемент 'NonExistentProduct' не знайдено (індекс = {index2})", "Неуспішний пошук");
            }
        }

        private void btnStep4_Click(object sender, EventArgs e)
        {
            if (!File.Exists("Product.xml"))
            {
                MessageBox.Show("Спочатку створіть Product.xml (Крок 1)");
                return;
            }

            var ds = new DataSet();
            using (XmlReader reader = XmlReader.Create("Product.xml"))
            {
                ds.ReadXml(reader);
            }

            DataView dv = ds.Tables[0].DefaultView;
            dv.RowFilter = "Product_Price >= 2000";

            DataTable resultTable = dv.ToTable();
            resultTable.WriteXml("Result.xml");

            dataGridView1.DataSource = dv;
            MessageBox.Show($"Відфільтровано {resultTable.Rows.Count} записів. Збережено в Result.xml");
        }

        private void btnStep5_Click(object sender, EventArgs e)
        {
            if (!File.Exists("Product.xml"))
            {
                MessageBox.Show("Файл Product.xml не знайдено");
                return;
            }

            var ds = new DataSet();
            using (XmlReader reader = XmlReader.Create("Product.xml"))
            {
                ds.ReadXml(reader);
            }

            int inserted = 0;
            int skipped = 0;

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    int id = Convert.ToInt32(row["Product_ID"]);
                    string name = row["Product_Name"].ToString() ?? "";
                    double price = Convert.ToDouble(row["Product_Price"]);

                    using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Product WHERE Product_ID = @id OR Product_Name = @name", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        checkCmd.Parameters.AddWithValue("@name", name);
                        long count = Convert.ToInt64(checkCmd.ExecuteScalar());

                        if (count == 0)
                        {
                            using (var insertCmd = new SQLiteCommand("INSERT INTO Product (Product_ID, Product_Name, Product_Price) VALUES (@id, @name, @price)", conn))
                            {
                                insertCmd.Parameters.AddWithValue("@id", id);
                                insertCmd.Parameters.AddWithValue("@name", name);
                                insertCmd.Parameters.AddWithValue("@price", price);
                                insertCmd.ExecuteNonQuery();
                                inserted++;
                            }
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                }
            }

            LoadData("SELECT * FROM Product");
            MessageBox.Show($"Імпорт завершено!\nДодано нових: {inserted}\nПропущено дублікатів: {skipped}");
        }
    }
}
