using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace shop
{
    public partial class Form1 : Form
    {
        private readonly string connStr;
        private FlowLayoutPanel flowProducts;

        public Form1()
        {
            InitializeComponent();

            connStr = ConfigurationManager.ConnectionStrings["shop"]?.ConnectionString
                      ?? throw new Exception("Không tìm thấy connectionString name='shop' trong App.config");

            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            EnsureFlowProducts();
            LoadProductsToCards();
        }

        // ================== TẠO FLOW PANEL (NẾU DESIGNER CHƯA CÓ) ==================
        private void EnsureFlowProducts()
        {
            // Nếu bạn đã tạo flowProducts trong Designer thì đoạn này sẽ tự lấy lại
            var found = this.Controls.Find("flowProducts", true);
            if (found.Length > 0 && found[0] is FlowLayoutPanel exist)
            {
                flowProducts = exist;
                return;
            }

            // Nếu chưa có -> tạo mới
            flowProducts = new FlowLayoutPanel();
            flowProducts.Name = "flowProducts";
            flowProducts.Dock = DockStyle.Fill;
            flowProducts.AutoScroll = true;
            flowProducts.WrapContents = true;
            flowProducts.FlowDirection = FlowDirection.LeftToRight;
            flowProducts.Padding = new Padding(10);

            // add vào Form (bạn có thể đổi chỗ add nếu muốn nằm trong panel nào đó)
            this.Controls.Add(flowProducts);
            flowProducts.BringToFront();
        }

        // ================== LOAD DATA -> CARD ==================
        private void LoadProductsToCards()
        {
            flowProducts.Controls.Clear();

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // ĐỔI tên bảng nếu bạn đang dùng [Sản phẩm] thay vì dbo.SanPham
                // string sql = "SELECT [MãSP] AS MaSP, [TênSP] AS TenSP, [Giá] AS Gia, [Hình] AS Hinh FROM [Sản phẩm]";
                string sql = "SELECT MaSP, TenSP, Gia, Hinh FROM dbo.SanPham";

                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    da.Fill(dt);
                }
            }

            foreach (DataRow r in dt.Rows)
            {
                string ma = r["MaSP"].ToString();
                string ten = r["TenSP"].ToString();
                decimal gia = Convert.ToDecimal(r["Gia"]);
                string hinh = r["Hinh"]?.ToString();

                var card = CreateProductCard(ma, ten, gia, hinh);
                flowProducts.Controls.Add(card);
            }
        }

        // Load ảnh không lock file
        private Image LoadImageNoLock(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var tmp = Image.FromStream(fs))
            {
                return (Image)tmp.Clone();
            }
        }

        private Control CreateProductCard(string ma, string ten, decimal gia, string hinhFile)
        {
            Panel card = new Panel();
            card.Width = 200;
            card.Height = 190;
            card.Margin = new Padding(10);
            card.BorderStyle = BorderStyle.FixedSingle;
            card.BackColor = Color.White;

            // Picture
            PictureBox pic = new PictureBox();
            pic.Width = 180;
            pic.Height = 100;
            pic.Left = 10;
            pic.Top = 10;
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = Color.Gainsboro;

            if (!string.IsNullOrWhiteSpace(hinhFile))
            {
                string path = Path.Combine(Application.StartupPath, "img", hinhFile);
                if (File.Exists(path))
                {
                    pic.Image = LoadImageNoLock(path);
                }
            }

            // Tên
            Label lblTen = new Label();
            lblTen.AutoSize = false;
            lblTen.Width = 180;
            lblTen.Height = 22;
            lblTen.Left = 10;
            lblTen.Top = 115;
            lblTen.Text = ten;
            lblTen.TextAlign = ContentAlignment.MiddleLeft;

            // Giá
            TextBox txtGia = new TextBox();
            txtGia.Width = 90;
            txtGia.Left = 10;
            txtGia.Top = 145;
            txtGia.ReadOnly = true;
            txtGia.Text = gia.ToString("N0");

            // Mua
            Button btnMua = new Button();
            btnMua.Width = 70;
            btnMua.Height = 26;
            btnMua.Left = 120;
            btnMua.Top = 143;
            btnMua.Text = "Mua";

            btnMua.Tag = ma;
            btnMua.Click += (s, e) =>
            {
                MessageBox.Show($"Bạn chọn mua: {ma} - {ten} - {gia:N0} đ");
            };

            card.Controls.Add(pic);
            card.Controls.Add(lblTen);
            card.Controls.Add(txtGia);
            card.Controls.Add(btnMua);

            return card;
        }

        // ================== CÁC HÀM NẾU DESIGNER ĐANG TRỎ SỰ KIỆN ==================
        private void button2_Click(object sender, EventArgs e)
        {
            using (var f = new Form2())
            {
                f.Owner = this;          // ⭐ để Form2 gọi ngược RefreshProducts()
                f.ShowDialog();          // ⭐ chờ Form2 đóng xong mới chạy tiếp
            }

            RefreshProducts();           // ✅ cập nhật lại danh sách sản phẩmard
        }

        // ✅ Hàm cập nhập lại danh sách sản phẩm trên Form1
        public void RefreshProducts()
        {
            EnsureFlowProducts();   // đảm bảo flowProducts đã được gán
            LoadProductsToCards();  // load lại từ DB và vẽ lại card
        }


        private void buttondathang_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Đặt hàng (chưa làm)");
        }

        private void buttonkhachhang_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Khách hàng (chưa làm)");
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            // để trống cũng được
        }
    }
}
