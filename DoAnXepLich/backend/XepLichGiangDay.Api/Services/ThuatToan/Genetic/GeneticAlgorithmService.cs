using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.XepLich;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class GeneticAlgorithmService(
    AppDbContext context,
    BoDanhGiaPhuongAn boDanhGia,
    TaoDanhSachUngVien taoUngVien,
    LuuKetQuaXepLichService luuKetQuaService) : ISchedulingAlgorithm
{
    public string TenThuatToan => "GA";

    public async Task<KetQuaXepLichDto> ChayAsync(DuLieuXepLichDto duLieu, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Nạp ngữ cảnh dữ liệu học kỳ vào bộ nhớ đệm
        var boiCanh = await boDanhGia.TaoBoiCanhAsync(duLieu.MaHocKy, cancellationToken);
        if (boiCanh.DanhSachLop.Count == 0)
        {
            return new KetQuaXepLichDto
            {
                ThuatToan = TenThuatToan,
                TrangThai = "KhongCoLopHocPhan",
                ThongDiep = "Học kỳ này chưa có lớp học phần nào để xếp lịch.",
                TongDiemPhat = 0
            };
        }

        // 2. Lấy danh sách lớp đã khóa ghim (H6/H8 - Integrity of locked assignments)
        var dsPhanCongHienTai = await context.PhanCongGiangDay
            .Where(x => x.MaHocKy == duLieu.MaHocKy)
            .ToListAsync(cancellationToken);

        var phanCongKhoa = dsPhanCongHienTai
            .Where(x => x.TrangThai == "DaKhoa")
            .GroupBy(x => x.MaLopHocPhan)
            .ToDictionary(g => g.Key, g => g.First().MaGiangVien);
        var cacLopCoDinh = phanCongKhoa.Keys.ToHashSet();

        // 3. XỬ LÝ GỘP CỤM LỚP GHÉP (Class Clustering / Joint Sessions Pre-processing)
        // Gom các lớp học phần học chung cùng môn, cùng thứ, tiết, tuần, phòng thành 1 ca dạy duy nhất
        string TaoChuKyCacBuoiHoc(int maLop, int maHocPhan)
        {
            if (!boiCanh.ChiTietTheoLop.TryGetValue(maLop, out var dsCt) || dsCt.Count == 0)
            {
                return $"HP_{maHocPhan}_LOP_{maLop}";
            }
            var chuoiBuoi = string.Join("|", dsCt
                .OrderBy(c => c.Thu)
                .ThenBy(c => c.TietBatDau)
                .ThenBy(c => c.TuTuan)
                .Select(c => $"{c.Thu}_{c.TietBatDau}_{c.TietKetThuc}_{c.TuTuan}_{c.DenTuan}_{c.PhongHoc?.Trim().ToUpperInvariant()}"));
            return $"HP_{maHocPhan}:{chuoiBuoi}";
        }

        var nhomLopGhep = boiCanh.DanhSachLop
            .GroupBy(lop => TaoChuKyCacBuoiHoc(lop.MaLopHocPhan, lop.MaHocPhan))
            .ToList();

        // Ánh xạ lớp đại diện -> Danh sách các lớp thành viên
        var mapLopGhep = new Dictionary<int, List<LopHocPhan>>();
        var dsLopDaiDien = new List<LopHocPhan>();
        var phanCongKhoaDaiDien = new Dictionary<int, int>();
        var cacLopCoDinhDaiDien = new HashSet<int>();

        foreach (var nhom in nhomLopGhep)
        {
            var dsThanhVien = nhom.ToList();
            // Ưu tiên chọn lớp nào đã bị khóa ghim làm đại diện
            var lopDaiDien = dsThanhVien.FirstOrDefault(l => cacLopCoDinh.Contains(l.MaLopHocPhan)) ?? dsThanhVien.First();

            mapLopGhep[lopDaiDien.MaLopHocPhan] = dsThanhVien;
            dsLopDaiDien.Add(lopDaiDien);

            // Đồng bộ khóa ghim: nếu bất kỳ lớp nào trong cụm bị khóa ghim, lớp đại diện cũng bị khóa theo GV đó
            var lopBiKhoa = dsThanhVien.FirstOrDefault(l => cacLopCoDinh.Contains(l.MaLopHocPhan));
            if (lopBiKhoa != null)
            {
                int gvKhoa = phanCongKhoa[lopBiKhoa.MaLopHocPhan];
                phanCongKhoaDaiDien[lopDaiDien.MaLopHocPhan] = gvKhoa;
                cacLopCoDinhDaiDien.Add(lopDaiDien.MaLopHocPhan);
            }
        }

        // Tạo bối cảnh rút gọn chỉ chứa các lớp đại diện phục vụ đường ống tiến hóa GA
        var boiCanhRutGon = new BoiCanhDanhGia
        {
            MaHocKy = boiCanh.MaHocKy,
            DanhSachLop = dsLopDaiDien,
            LopTheoMa = dsLopDaiDien.ToDictionary(l => l.MaLopHocPhan),
            ChiTietTheoLop = boiCanh.ChiTietTheoLop,
            GiangVienTheoMa = boiCanh.GiangVienTheoMa,
            NangLucTheoGv = boiCanh.NangLucTheoGv,
            LichBanTheoGv = boiCanh.LichBanTheoGv,
            NguyenVongTheoGv = boiCanh.NguyenVongTheoGv,
            DinhMucTheoGv = boiCanh.DinhMucTheoGv,
            TrongSoRangBuoc = boiCanh.TrongSoRangBuoc
        };

        // 4. Tạo danh sách ứng viên chuyên môn cho từng lớp học phần
        var dsNangLuc = await context.NangLucGiangDay
            .Where(x => string.IsNullOrEmpty(x.TrangThai) || x.TrangThai == "DangApDung")
            .ToListAsync(cancellationToken);

        var ungVienTheoLop = taoUngVien.TaoToanBo(
            boiCanh.DanhSachLop,
            boiCanh.ChiTietTheoLop.Values.SelectMany(x => x).ToList(),
            dsNangLuc,
            boiCanh.LichBanTheoGv.Values.SelectMany(x => x).ToList(),
            boiCanh.GiangVienTheoMa.Values.ToList());

        // 5. TIỀN KIỂM TRA TẬP ỨNG VIÊN (Domain Feasibility Check)
        // Kiểm tra trên danh sách các lớp đại diện
        var cacLopKhongCoUngVien = new List<string>();
        foreach (var lop in dsLopDaiDien)
        {
            if (cacLopCoDinhDaiDien.Contains(lop.MaLopHocPhan)) continue;

            if (!ungVienTheoLop.TryGetValue(lop.MaLopHocPhan, out var dsUv) || dsUv.Count == 0)
            {
                cacLopKhongCoUngVien.Add($"Lớp [{lop.MaLopHocPhanTruong}] (Mã HP: {lop.MaHocPhan}) không có bất kỳ giảng viên nào đủ năng lực chuyên môn.");
            }
        }

        if (cacLopKhongCoUngVien.Count > 0)
        {
            sw.Stop();
            return new KetQuaXepLichDto
            {
                ThuatToan = TenThuatToan,
                TrangThai = "KhongCoUngVienHopLe",
                TongDiemPhat = 999999,
                SoViPhamCung = cacLopKhongCoUngVien.Count,
                ThoiGianChayMiligiay = sw.ElapsedMilliseconds,
                ThoiGianChayGiay = Math.Round(sw.Elapsed.TotalSeconds, 2),
                ThongDiep = $"Phát hiện {cacLopKhongCoUngVien.Count} lớp học phần không có giảng viên nào đủ điều kiện chuyên môn. Không thể tìm được nghiệm hợp lệ.",
                DanhSachViPham = cacLopKhongCoUngVien
            };
        }

        // 6. Chuẩn bị bảng thời gian các lớp và lịch bận giảng viên phục vụ toán tử sửa lỗi & Heuristic Seeding
        var thoiGianTheoLop = boiCanh.ChiTietTheoLop.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(ct => new ThoiGianLop
            {
                MaLopHocPhan = ct.MaLopHocPhan,
                Thu = ct.Thu,
                TietBatDau = ct.TietBatDau,
                TietKetThuc = ct.TietKetThuc,
                TuTuan = ct.TuTuan > 0 ? ct.TuTuan : 1,
                DenTuan = ct.DenTuan > 0 ? ct.DenTuan : 53
            }).ToList()
        );

        var lichBanTheoGv = boiCanh.LichBanTheoGv.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(lb => new ThoiGianBan
            {
                Thu = lb.Thu,
                TietBatDau = lb.TietBatDau,
                TietKetThuc = lb.TietKetThuc
            }).ToList()
        );

        // 7. Cấu hình tham số tiến hóa GA chuẩn hóa
        var cauHinh = new CauHinhGa
        {
            KichThuocQuanThe = duLieu.KichThuocQuanThe.GetValueOrDefault(80),
            SoTheHe = duLieu.SoTheHe.GetValueOrDefault(350),
            TyLeLaiGhep = duLieu.TyLeLaiGhep.GetValueOrDefault(0.85),
            TyLeDotBien = duLieu.TyLeDotBien.GetValueOrDefault(0.08),
            KichThuocGiaiDau = duLieu.KichThuocGiaiDau.GetValueOrDefault(4),
            SoCaTheUuTu = duLieu.SoCaTheUuTu.GetValueOrDefault(3),
            RandomSeed = duLieu.RandomSeed,
            ThoiGianChayToiDaGiay = duLieu.ThoiGianChayToiDa.GetValueOrDefault(30.0)
        };
        cauHinh.ChuanHoaVaXacThuc();

        // Khởi tạo Random với seed (nếu có) để đảm bảo tính tái lập 100%
        var random = cauHinh.RandomSeed.HasValue ? new Random(cauHinh.RandomSeed.Value) : new Random();

        var khoiTao = new KhoiTaoQuanThe(random);
        var chonLoc = new ChonLoc(random);
        var laiGhep = new LaiGhep(random);
        var dotBien = new DotBien(random);
        var suaCaThe = new SuaCaThe(random);

        var danhSachMaLopDaiDien = dsLopDaiDien.Select(x => x.MaLopHocPhan).ToList();

        // 8. Khởi tạo quần thể ban đầu kết hợp Heuristic Seeding & Random trên tập lớp đại diện
        var quanThe = khoiTao.TaoQuanTheBanDau(
            cauHinh.KichThuocQuanThe,
            danhSachMaLopDaiDien,
            ungVienTheoLop,
            phanCongKhoaDaiDien,
            thoiGianTheoLop,
            lichBanTheoGv);

        void DanhGiaQuanThe(List<CaThe> danhSach)
        {
            foreach (var ct in danhSach)
            {
                var dictPhanCong = ct.DanhSachGen.ToDictionary(g => g.MaLopHocPhan, g => (int?)g.MaGiangVien);
                var kq = boDanhGia.DanhGia(dictPhanCong, boiCanhRutGon);
                ct.KetQuaDanhGia = kq;
                ct.DiemPhat = kq.TongDiemPhat;
                ct.SoViPhamCung = kq.SoViPhamCung;
                ct.TongDiemPhatMem = kq.TongDiemPhatMem;
                ct.DoThichNghi = kq.DoThichNghi;
            }
        }

        // Đánh giá quần thể thế hệ 0
        DanhGiaQuanThe(quanThe);
        quanThe.Sort((a, b) => a.SoSanhVoi(b));

        var bestOverall = SaoChepCaThe(quanThe[0]);
        var lichSuDiemPhat = new List<double> { bestOverall.DiemPhat };

        int theHeThucTe = 0;
        int soTheHeKhongCaiThien = 0;
        const int NguongTriTre = 75;

        // 9. Vòng lặp tiến hóa qua các thế hệ
        for (int gen = 1; gen <= cauHinh.SoTheHe; gen++)
        {
            theHeThucTe = gen;
            cancellationToken.ThrowIfCancellationRequested();

            // Kiểm tra giới hạn thời gian (Time Limit)
            if (sw.Elapsed.TotalSeconds >= cauHinh.ThoiGianChayToiDaGiay)
            {
                break;
            }

            // Dừng sớm nếu đã tìm thấy phương án tối ưu tuyệt đối (hợp lệ và không còn điểm phạt mềm)
            if (bestOverall.LaHopLe && bestOverall.TongDiemPhatMem <= 0.0001)
            {
                break;
            }

            var theHeMoi = new List<CaThe>(cauHinh.KichThuocQuanThe);

            // Elitism: Bảo toàn k cá thể ưu tú nhất
            int eliteCount = Math.Min(cauHinh.SoCaTheUuTu, quanThe.Count);
            for (int e = 0; e < eliteCount; e++)
            {
                theHeMoi.Add(SaoChepCaThe(quanThe[e]));
            }

            // Cơ chế Đột biến thích nghi (Adaptive Mutation): tăng nhẹ tỷ lệ đột biến khi bị kẹt ở nghiệm vi phạm cứng
            double tyLeDotBienHienTai = cauHinh.TyLeDotBien;
            if (soTheHeKhongCaiThien >= 8 && !bestOverall.LaHopLe)
            {
                tyLeDotBienHienTai = Math.Min(0.20, cauHinh.TyLeDotBien * 2.0);
            }

            // Sinh các cá thể mới bằng: Selection -> Crossover -> Mutation -> Repair
            while (theHeMoi.Count < cauHinh.KichThuocQuanThe)
            {
                var cha = chonLoc.ChonCaTheChaMe(quanThe, cauHinh.KichThuocGiaiDau);
                var me = chonLoc.ChonCaTheChaMe(quanThe, cauHinh.KichThuocGiaiDau);

                var con = laiGhep.TaoCon(cha, me, cauHinh.TyLeLaiGhep, cacLopCoDinhDaiDien);
                con = dotBien.ThucHien(con, tyLeDotBienHienTai, ungVienTheoLop, cacLopCoDinhDaiDien);
                con = suaCaThe.SuaNeuViPham(con, thoiGianTheoLop, ungVienTheoLop, cacLopCoDinhDaiDien, lichBanTheoGv);

                theHeMoi.Add(con);
            }

            // Đánh giá thế hệ mới và sắp xếp theo thứ tự phân tầng (Hard > Soft)
            DanhGiaQuanThe(theHeMoi);
            theHeMoi.Sort((a, b) => a.SoSanhVoi(b));
            quanThe = theHeMoi;

            // Cập nhật cá thể tốt nhất toàn cục
            if (quanThe[0].SoSanhVoi(bestOverall) < 0)
            {
                bestOverall = SaoChepCaThe(quanThe[0]);
                soTheHeKhongCaiThien = 0;
            }
            else
            {
                soTheHeKhongCaiThien++;
            }

            lichSuDiemPhat.Add(bestOverall.DiemPhat);

            // Dừng sớm khi quần thể đã hội tụ (nghiệm hợp lệ và không cải thiện sau N thế hệ liên tiếp)
            if (bestOverall.LaHopLe && soTheHeKhongCaiThien >= NguongTriTre)
            {
                break;
            }
        }

        sw.Stop();

        // 10. HẬU XỬ LÝ (Post-processing Expansion): Bung nghiệm từ các lớp đại diện ra toàn bộ các lớp thành viên
        var danhSachGenDayDu = new List<Gen>();
        foreach (var genDd in bestOverall.DanhSachGen)
        {
            if (mapLopGhep.TryGetValue(genDd.MaLopHocPhan, out var dsThanhVien))
            {
                foreach (var lopTv in dsThanhVien)
                {
                    danhSachGenDayDu.Add(new Gen
                    {
                        MaLopHocPhan = lopTv.MaLopHocPhan,
                        MaGiangVien = genDd.MaGiangVien
                    });
                }
            }
            else
            {
                danhSachGenDayDu.Add(new Gen
                {
                    MaLopHocPhan = genDd.MaLopHocPhan,
                    MaGiangVien = genDd.MaGiangVien
                });
            }
        }
        bestOverall.DanhSachGen = danhSachGenDayDu;

        // Đánh giá kiểm tra độc lập toàn bộ phương án đã bung trên bối cảnh học kỳ gốc đầy đủ
        var dictNghiemCuoiDayDu = bestOverall.DanhSachGen.ToDictionary(g => g.MaLopHocPhan, g => (int?)g.MaGiangVien);
        var finalCheck = boDanhGia.DanhGia(dictNghiemCuoiDayDu, boiCanh);
        bestOverall.KetQuaDanhGia = finalCheck;
        bestOverall.DiemPhat = finalCheck.TongDiemPhat;
        bestOverall.SoViPhamCung = finalCheck.SoViPhamCung;
        bestOverall.TongDiemPhatMem = finalCheck.TongDiemPhatMem;
        bestOverall.DoThichNghi = finalCheck.DoThichNghi;

        var dsPhanCongDto = new List<PhanCongDto>();
        foreach (var gen in bestOverall.DanhSachGen)
        {
            if (gen.MaGiangVien.HasValue && gen.MaGiangVien.Value > 0)
            {
                double diemPhatLop = boDanhGia.TinhDiemNguyenVong(gen.MaGiangVien.Value, gen.MaLopHocPhan, boiCanh);
                dsPhanCongDto.Add(new PhanCongDto
                {
                    MaLopHocPhan = gen.MaLopHocPhan,
                    MaGiangVien = gen.MaGiangVien.Value,
                    DiemPhat = Math.Round(diemPhatLop, 1)
                });
            }
        }

        // 10. GHI ĐÈ KẾT QUẢ VÀO CSDL (Đảm bảo đồ án luôn lưu và hiển thị kết quả của thuật toán vừa chạy)
        var dictPhanCongCuoi = bestOverall.DanhSachGen.ToDictionary(g => g.MaLopHocPhan, g => (int?)g.MaGiangVien);
        var ketQuaLuu = await luuKetQuaService.LuuPhanCongAsync(
            duLieu.MaHocKy,
            TenThuatToan,
            dictPhanCongCuoi,
            boiCanh,
            cancellationToken);

        string thongBaoKhoa = ketQuaLuu.SoLopGiuKhoa > 0
            ? $" ({ketQuaLuu.SoLopGiuKhoa} lớp đã khóa được giữ nguyên phân công)."
            : "";

        string trangThaiKetQua;
        string thongDiepKetQua;

        if (finalCheck.SoViPhamCung > 0)
        {
            trangThaiKetQua = "ConViPham";
            thongDiepKetQua = $"Thuật toán GA đã chạy qua {theHeThucTe} thế hệ và đã lưu kết quả vào CSDL (Nguồn: GA). CẢNH BÁO: Còn {finalCheck.SoViPhamCung} vi phạm ràng buộc cứng (Tổng điểm phạt: {Math.Round(finalCheck.TongDiemPhat, 1)})." + thongBaoKhoa;
        }
        else if (finalCheck.TongDiemPhatMem <= 0.0001)
        {
            trangThaiKetQua = "ToiUuTuyetDoi";
            thongDiepKetQua = $"GA tìm thấy phương án tối ưu tuyệt đối (điểm phạt = 0) tại thế hệ {theHeThucTe}." + thongBaoKhoa;
        }
        else
        {
            trangThaiKetQua = "ThoaManRangBuocCung";
            thongDiepKetQua = $"GA tìm thấy phương án thỏa mãn 100% ràng buộc cứng sau {theHeThucTe} thế hệ (Tổng điểm phạt mềm: {Math.Round(finalCheck.TongDiemPhatMem, 1)})." + thongBaoKhoa;
        }

        return new KetQuaXepLichDto
        {
            ThuatToan = TenThuatToan,
            TongDiemPhat = finalCheck.TongDiemPhat,
            DoThichNghi = finalCheck.DoThichNghi,
            DiemPhatRangBuocCung = finalCheck.DiemPhatRangBuocCung,
            DiemPhatNguyenVong = finalCheck.DiemPhatNguyenVong,
            DiemPhatCanBangTai = finalCheck.DiemPhatCanBangTai,
            DiemPhatGomLich = finalCheck.DiemPhatGomLich,
            SoViPhamCung = finalCheck.SoViPhamCung,
            SoLopChuaPhanCong = finalCheck.SoLopChuaPhanCong,
            SoTheHeThucTe = theHeThucTe,
            ThoiGianChayMiligiay = sw.ElapsedMilliseconds,
            ThoiGianChayGiay = Math.Round(sw.Elapsed.TotalSeconds, 2),
            TrangThai = trangThaiKetQua,
            ThongDiep = thongDiepKetQua,
                LichSuDiemPhatTheoTheHe = lichSuDiemPhat,
                DanhSachViPham = finalCheck.DanhSachViPham,
                ChiTietViPham = finalCheck.DanhSachCanhBaoMem,
                DanhSachPhanCong = dsPhanCongDto
            };
        }

        // KHI NGHIỆM THỎA MÃN 100% RÀNG BUỘC CỨNG: Cập nhật CSDL an toàn qua LuuKetQuaXepLichService
        var dictPhanCongCuoi = bestOverall.DanhSachGen.ToDictionary(g => g.MaLopHocPhan, g => (int?)g.MaGiangVien);
        var ketQuaLuu = await luuKetQuaService.LuuPhanCongAsync(
            duLieu.MaHocKy,
            TenThuatToan,
            dictPhanCongCuoi,
            boiCanh,
            cancellationToken);

        string thongBaoKhoa = ketQuaLuu.SoLopGiuKhoa > 0
            ? $" ({ketQuaLuu.SoLopGiuKhoa} lớp đã khóa được giữ nguyên phân công)."
            : "";

        string trangThaiKetQua = finalCheck.TongDiemPhatMem <= 0.0001
            ? "ToiUuTuyetDoi"
            : "ThoaManRangBuocCung";

        return new KetQuaXepLichDto
        {
            ThuatToan = TenThuatToan,
            TongDiemPhat = finalCheck.TongDiemPhat,
            DoThichNghi = finalCheck.DoThichNghi,
            DiemPhatRangBuocCung = finalCheck.DiemPhatRangBuocCung,
            DiemPhatNguyenVong = finalCheck.DiemPhatNguyenVong,
            DiemPhatCanBangTai = finalCheck.DiemPhatCanBangTai,
            DiemPhatGomLich = finalCheck.DiemPhatGomLich,
            SoViPhamCung = 0,
            SoLopChuaPhanCong = finalCheck.SoLopChuaPhanCong,
            SoTheHeThucTe = theHeThucTe,
            ThoiGianChayMiligiay = sw.ElapsedMilliseconds,
            ThoiGianChayGiay = Math.Round(sw.Elapsed.TotalSeconds, 2),
            TrangThai = trangThaiKetQua,
            ThongDiep = (trangThaiKetQua == "ToiUuTuyetDoi"
                ? $"GA tìm thấy phương án tối ưu tuyệt đối (điểm phạt = 0) tại thế hệ {theHeThucTe}."
                : $"GA tìm thấy phương án thỏa mãn 100% ràng buộc cứng sau {theHeThucTe} thế hệ (Tổng điểm phạt mềm: {Math.Round(finalCheck.TongDiemPhatMem, 1)}).") + thongBaoKhoa,
            LichSuDiemPhatTheoTheHe = lichSuDiemPhat,
            DanhSachViPham = finalCheck.DanhSachViPham,
            ChiTietViPham = finalCheck.DanhSachCanhBaoMem,
            DanhSachPhanCong = dsPhanCongDto
        };
    }

    private static CaThe SaoChepCaThe(CaThe goc)
    {
        return new CaThe
        {
            DiemPhat = goc.DiemPhat,
            SoViPhamCung = goc.SoViPhamCung,
            TongDiemPhatMem = goc.TongDiemPhatMem,
            DoThichNghi = goc.DoThichNghi,
            KetQuaDanhGia = goc.KetQuaDanhGia,
            DanhSachGen = goc.DanhSachGen.Select(g => new Gen
            {
                MaLopHocPhan = g.MaLopHocPhan,
                MaGiangVien = g.MaGiangVien
            }).ToList()
        };
    }
}
