export interface LichBan {
  maLichBan: number
  maGiangVien: number
  tenGiangVien?: string
  maBoMon?: number
  tenBoMon?: string
  maHocKy: number
  thu: number
  tietBatDau: number
  tietKetThuc: number
  lyDo?: string
  laSeminar?: boolean
}

export interface TaoLichSeminarRequest {
  maHocKy: number
  tenSeminar: string
  thu: number
  tietBatDau: number
  tietKetThuc: number
  danhSachMaGiangVien: number[]
}

export interface KetQuaTaoLichSeminar {
  thanhCong: boolean
  thongDiep: string
  soGiangVienDaKhoa: number
  soGiangVienDaTonTai: number
  soLopBiGo: number
  danhSachLopBiGo: string[]
}

export interface XoaNhomSeminarRequest {
  maHocKy: number
  thu: number
  tietBatDau: number
  tietKetThuc: number
  lyDo?: string
  maBoMon?: number
}

export interface TaoLichBanDto {
  thu: number
  tietBatDau: number
  tietKetThuc: number
  lyDo?: string
}

export interface NguyenVong {
  maNguyenVong: number
  maGiangVien: number
  tenGiangVien?: string
  maBoMon?: number
  tenBoMon?: string
  maHocKy: number
  maRangBuocMem: string
  tenRangBuocMem: string
  loaiNguyenVong: string
  mucDo: number
  khoaCungLichBan?: boolean
  thu?: number
  tietBatDau?: number
  tietKetThuc?: number
  trongSoPhat: number
  noiDungGoc?: string
  lyDo?: string
  trangThai?: 'ChoDuyet' | 'DaDuyet' | 'TuChoi' | string
  ngayTao?: string
  ngayDuyet?: string
  ghiChuDuyet?: string
}

export interface KetQuaDuyetNguyenVong {
  thanhCong: boolean
  thongBao: string
  soLopBiHuy: number
  danhSachLopBiHuy: string[]
  trangThai: string
}

export interface ThemNguyenVongRequest {
  maHocKy: number
  maGiangVien: number
  maRangBuocMem: string
  tenRangBuocMem?: string
  thu?: number
  tietBatDau?: number
  tietKetThuc?: number
  tuTuan?: number
  denTuan?: number
  trongSoPhat: number
  lyDo?: string
  khoaCungLichBan?: boolean
}

export interface TaoNguyenVongDto {
  loaiNguyenVong: 'ThichDay' | 'TranhDay' | string
  mucDo: number
  thu?: number
  tietBatDau?: number
  tietKetThuc?: number
  ghiChu?: string
}

export interface LuuMaTranNguyenVongRequest {
  maGiangVien: number
  maHocKy: number
  danhSachLichBan: TaoLichBanDto[]
  danhSachNguyenVong: TaoNguyenVongDto[]
}

export type TrangThaiTiet = 'FREE' | 'BUSY' | 'PREFERRED' | 'AVOID'

export interface RangBuocMemItem {
  maRb: string
  tenRb: string
  diemPhatMacDinh: number
  thuMacDinh?: number
  tietBdMacDinh?: number
  tietKtMacDinh?: number
  moTa: string
}

export const DANH_MUC_RANG_BUOC_MEM: RangBuocMemItem[] = [
  {
    maRb: 'SC01',
    tenRb: 'Tránh khung giờ / tiết bận cụ thể',
    diemPhatMacDinh: 15.0,
    thuMacDinh: 2,
    tietBdMacDinh: 1,
    tietKtMacDinh: 5,
    moTa: 'Giảng viên bận công việc vào khoảng tiết và thứ nhất định (hoặc cả tuần)',
  },
  {
    maRb: 'SC02',
    tenRb: 'Tránh dạy ca tối (Tiết 10 - 15)',
    diemPhatMacDinh: 10.0,
    thuMacDinh: 0,
    tietBdMacDinh: 10,
    tietKtMacDinh: 15,
    moTa: 'Không xếp lịch vào ca chiều muộn / tối',
  },
  {
    maRb: 'SC03',
    tenRb: 'Tránh dạy ngày Thứ Bảy / Chủ Nhật',
    diemPhatMacDinh: 12.0,
    thuMacDinh: 7,
    tietBdMacDinh: 1,
    tietKtMacDinh: 15,
    moTa: 'Giảng viên xin nghỉ hoặc tránh dạy cuối tuần',
  },
  {
    maRb: 'SC04',
    tenRb: 'Ưu tiên dạy buổi sáng (Tiết 1 - 6)',
    diemPhatMacDinh: 8.0,
    thuMacDinh: 0,
    tietBdMacDinh: 1,
    tietKtMacDinh: 6,
    moTa: 'Giảng viên mong muốn tập trung dạy ca sáng',
  },
  {
    maRb: 'SC05',
    tenRb: 'Ưu tiên dạy buổi chiều (Tiết 7 - 12)',
    diemPhatMacDinh: 8.0,
    thuMacDinh: 0,
    tietBdMacDinh: 7,
    tietKtMacDinh: 12,
    moTa: 'Giảng viên mong muốn tập trung dạy ca chiều',
  },
  {
    maRb: 'SC06',
    tenRb: 'Giới hạn số tiết tối đa / ngày',
    diemPhatMacDinh: 10.0,
    thuMacDinh: 0,
    tietBdMacDinh: 1,
    tietKtMacDinh: 6,
    moTa: 'Tránh quá tải dạy quá nhiều tiết trong 1 ngày',
  },
  {
    maRb: 'SC07',
    tenRb: 'Hạn chế số ngày dạy trong tuần (gom lịch)',
    diemPhatMacDinh: 20.0,
    thuMacDinh: 0,
    tietBdMacDinh: 1,
    tietKtMacDinh: 15,
    moTa: 'Gom các buổi dạy lại, tránh đi dạy rải rác',
  },
]
