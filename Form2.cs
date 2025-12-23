using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace shop
{
    public partial class Form2 : Form
    {
        private readonly string connStr;
        private string selectedImageFileName = null; // tên file ảnh lưu trong DB (vd: ao.png)

        public Form2()
        {
            InitializeComponent();

            connStr = ConfigurationManager.ConnectionStrings["shop"]?.ConnectionString
                      ?? throw new Exception("Không tìm thấy connectionString name='shop' trong App.config");

            // ===== realtime preview =====
            txtMa.TextChanged += Input_Changed;
            txtTen.TextChanged += Input_Changed;
            txtGia.TextChanged += Input_Changed;

            // ===== events =====
            // (Designer của bạn đang gắn Form.Load = Form1_Load, nên mình dùng đúng tên)
            // btnChonAnh / btnLuu / btnsua / btnXoa: nếu bạn đã gắn trong designer thì không sao, gắn thêm cũng OK.
            btnChonAnh.Click += btnChonAnh_Click;
            btnLuu.Click += btnLuu_Click;
            btnsua.Click += btnsua_Click;

            // Nếu nút Xóa của bạn tên khác (vd btnHuy), đổi lại đúng tên control
            // Hoặc trong Designer gắn Click -> btnXoa_Click
            // btnXoa.Click += btnXoa_Click;

            dgvSanPham.CellClick += dgvSanPham_CellClick;
        }

        // =============== FORM LOAD (đúng tên theo Designer bạn gửi: Form1_Load) ===============
        private void Form1_Load(object sender, EventArgs e)
        {
            SetupGrid();
            LoadData();
            UpdatePreview();
        }

        // ================== GRID ==================
        private void SetupGrid()
        {
            dgvSanPham.AutoGenerateColumns = false;
            dgvSanPham.Columns.Clear();
            dgvSanPham.AllowUserToAddRows = false;
            dgvSanPham.ReadOnly = true;
            dgvSanPham.RowTemplate.Height = 80;
            dgvSanPham.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var colImg = new DataGridViewImageColumn
            {
                Name = "colImg",
                HeaderText = "Ảnh",
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                Width = 90
            };
            dgvSanPham.Columns.Add(colImg);

            dgvSanPham.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaSP", HeaderText = "Mã SP" });
            dgvSanPham.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenSP", HeaderText = "Tên SP" });
            dgvSanPham.Columns.Add(new DataGridViewTextBoxColumn { Name = "Gia", HeaderText = "Giá" });
        }

        private static Image LoadImageNoLock(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var temp = Image.FromStream(fs))
            {
                return (Image)temp.Clone();
            }
        }

        // ================== LOAD DATA FROM SQL ==================
        private void LoadData()
        {
            dgvSanPham.Rows.Clear();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // BẢNG: dbo.SanPham (MaSP, TenSP, Gia, Hinh)
                string sql = "SELECT MaSP, TenSP, Gia, Hinh FROM dbo.SanPham ORDER BY MaSP";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        Image img = null;
                        string tenAnh = rd["Hinh"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(tenAnh))
                        {
                            string path = Path.Combine(Application.StartupPath, "img", tenAnh);
                            if (File.Exists(path))
                                img = LoadImageNoLock(path);
                        }

                        dgvSanPham.Rows.Add(
                            img,
                            rd["MaSP"].ToString(),
                            rd["TenSP"].ToString(),
                            Convert.ToDecimal(rd["Gia"]).ToString("N0") + " đ"
                        );
                    }
                }
            }
        }

        // =============== CLICK GRID -> ĐỔ DỮ LIỆU LÊN Ô NHẬP + PREVIEW ===============
        private void dgvSanPham_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvSanPham.Rows[e.RowIndex];

            txtMa.Text = row.Cells["MaSP"].Value?.ToString() ?? "";
            txtTen.Text = row.Cells["TenSP"].Value?.ToString() ?? "";

            string giaText = (row.Cells["Gia"].Value?.ToString() ?? "")
                .Replace("đ", "").Replace("Đ", "")
                .Replace(",", "").Replace(".", "").Trim();
            txtGia.Text = giaText;

            // ảnh
            if (row.Cells["colImg"].Value is Image cellImg)
            {
                picPreview.Image = (Image)cellImg.Clone();
                picPreview.SizeMode = PictureBoxSizeMode.Zoom;
                // lưu tạm: khi sửa mà không chọn ảnh mới thì giữ nguyên tên ảnh cũ (không bắt buộc)
            }
            else
            {
                picPreview.Image = null;
            }

            UpdatePreview();
        }

        // ================== PREVIEW REALTIME ==================
        private void Input_Changed(object sender, EventArgs e) => UpdatePreview();

        private void UpdatePreview()
        {
            lblMa.Text = txtMa.Text.Trim();
            lblTen.Text = txtTen.Text.Trim();

            if (decimal.TryParse(txtGia.Text.Trim(), out decimal gia))
                lblGia.Text = gia.ToString("N0") + " đ";
            else
                lblGia.Text = txtGia.Text.Trim();
        }

        // ================== CHỌN ẢNH (LƯU VÀO THƯ MỤC img + LƯU TÊN FILE) ==================
        private void btnChonAnh_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image|*.png;*.jpg;*.jpeg";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                string imgFolder = Path.Combine(Application.StartupPath, "img");
                Directory.CreateDirectory(imgFolder);

                selectedImageFileName = Path.GetFileName(ofd.FileName);
                string dest = Path.Combine(imgFolder, selectedImageFileName);

                // copy đè để đảm bảo luôn có file
                File.Copy(ofd.FileName, dest, true);

                // preview giữa
                picPreview.Image = LoadImageNoLock(dest);
                picPreview.SizeMode = PictureBoxSizeMode.Zoom;

                // preview nhỏ bên trái (pictureBox1)
                if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
                pictureBox1.Image = LoadImageNoLock(dest);
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

                UpdatePreview();
            }
        }

        // ================== LƯU (INSERT/UPDATE) ==================
        private void btnLuu_Click(object sender, EventArgs e)
        {
            string ma = txtMa.Text.Trim();
            string ten = txtTen.Text.Trim();

            if (string.IsNullOrWhiteSpace(ma) || string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(txtGia.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã SP, Tên SP, Giá.");
                return;
            }

            if (!decimal.TryParse(txtGia.Text.Trim(), out decimal gia))
            {
                MessageBox.Show("Giá không hợp lệ.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
IF EXISTS (SELECT 1 FROM dbo.SanPham WHERE MaSP = @MaSP)
BEGIN
    UPDATE dbo.SanPham
    SET TenSP = @TenSP,
        Gia   = @Gia,
        Hinh  = @Hinh
    WHERE MaSP = @MaSP
END
ELSE
BEGIN
    INSERT INTO dbo.SanPham(MaSP, TenSP, Gia, Hinh)
    VALUES(@MaSP, @TenSP, @Gia, @Hinh)
END";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaSP", ma);
                    cmd.Parameters.AddWithValue("@TenSP", ten);
                    cmd.Parameters.AddWithValue("@Gia", gia);

                    // nếu chưa chọn ảnh mới thì vẫn cho NULL (hoặc giữ ảnh cũ tuỳ bạn)
                    cmd.Parameters.AddWithValue("@Hinh",
                        string.IsNullOrWhiteSpace(selectedImageFileName) ? (object)DBNull.Value : selectedImageFileName);

                    cmd.ExecuteNonQuery();
                }
            }

            LoadData();
            if (this.Owner is Form1 f1)
            {
                f1.RefreshProducts();
            }

            MessageBox.Show("Đã lưu sản phẩm!");
        }

        // ================== SỬA (UPDATE) ==================
        private void btnsua_Click(object sender, EventArgs e)
        {
            string ma = txtMa.Text.Trim();
            if (string.IsNullOrWhiteSpace(ma))
            {
                MessageBox.Show("Chưa chọn sản phẩm cần sửa.");
                return;
            }

            if (!decimal.TryParse(txtGia.Text.Trim(), out decimal gia))
            {
                MessageBox.Show("Giá không hợp lệ.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
UPDATE dbo.SanPham
SET TenSP = @TenSP,
    Gia   = @Gia,
    Hinh  = @Hinh
WHERE MaSP = @MaSP";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaSP", ma);
                    cmd.Parameters.AddWithValue("@TenSP", txtTen.Text.Trim());
                    cmd.Parameters.AddWithValue("@Gia", gia);
                    cmd.Parameters.AddWithValue("@Hinh",
                        string.IsNullOrWhiteSpace(selectedImageFileName) ? (object)DBNull.Value : selectedImageFileName);

                    int n = cmd.ExecuteNonQuery();
                    if (n == 0)
                    {
                        MessageBox.Show("Không tìm thấy mã sản phẩm để sửa.");
                        return;
                    }
                }
            }

            LoadData();
            if (this.Owner is Form1 f1)
            {
                f1.RefreshProducts();
            }

            MessageBox.Show("Sửa sản phẩm thành công!");
        }
        // ====== STUB EVENTS (Designer đang gọi) ======

        private void lblTen_Click(object sender, EventArgs e)
        {
            // không cần code
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            // nếu nút Hủy của bạn đang dùng để XÓA thì gọi hàm xóa
            btnXoa_Click(sender, e);

            // hoặc nếu bạn muốn Hủy = clear form thì dùng:
            // txtMa.Clear(); txtTen.Clear(); txtGia.Clear();
            // selectedImageFileName = null;
            // picPreview.Image = null;
            // pictureBox1.Image = null;
            // UpdatePreview();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            // không cần code
        }
        // Designer đang gọi Form2_Load -> mình cho nó gọi lại Form1_Load (hoặc code load bạn đang dùng)
        private void Form2_Load(object sender, EventArgs e)
        {
            Form1_Load(sender, e); // nếu bạn đang dùng Form1_Load để setup/load dữ liệu
        }
        // Designer đang gọi btnChonAnh_Click_1 -> mình cho nó gọi lại btnChonAnh_Click
        private void btnChonAnh_Click_1(object sender, EventArgs e)
        {
            btnChonAnh_Click(sender, e);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // không cần code
        }
       




        private void thoátToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ================== XÓA (DELETE) ==================
        // Đổi tên nút trong form của bạn thành btnXoa và gắn Click -> btnXoa_Click
        private void btnXoa_Click(object sender, EventArgs e)
        {
            string ma = txtMa.Text.Trim();
            if (string.IsNullOrWhiteSpace(ma))
            {
                MessageBox.Show("Chưa chọn sản phẩm cần xóa.");
                return;
            }

            if (MessageBox.Show("Xóa sản phẩm này?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "DELETE FROM dbo.SanPham WHERE MaSP = @MaSP";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaSP", ma);
                    cmd.ExecuteNonQuery();
                }
            }

            // clear UI
            txtMa.Clear(); txtTen.Clear(); txtGia.Clear();
            selectedImageFileName = null;
            if (picPreview.Image != null) { picPreview.Image.Dispose(); picPreview.Image = null; }
            if (pictureBox1.Image != null) { pictureBox1.Image.Dispose(); pictureBox1.Image = null; }
            lblMa.Text = "mã"; lblTen.Text = "tên"; lblGia.Text = "giá";

            LoadData();
            MessageBox.Show("Đã xóa sản phẩm!");
            if (this.Owner is Form1 f1)
            {
                f1.RefreshProducts();
            }


        }


        // ====== CÁC HÀM “CHO KHỎI BÁO LỖI” DO DESIGNER ĐANG TRỎ TỚI ======
        private void dgvSanPham_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void txtMa_TextChanged(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void picPreview_Click(object sender, EventArgs e) { }

        private void btnLuu_Click_1(object sender, EventArgs e)
        {

        }
    }
}
