export interface NguoiDung {
  maNguoiDung: number
  tenDangNhap: string
  hoTen: string
  email?: string
  vaiTro: string // Admin, TruongBoMon, GiangVien
  maGiangVien?: number
  tenGiangVien?: string
  maBoMon?: number
  tenBoMon?: string
  trangThai: string // KichHoat, Khoa
}

export interface TaoNguoiDungRequest {
  tenDangNhap: string
  matKhau: string
  hoTen: string
  email?: string
  vaiTro: string
  maGiangVien?: number
  maBoMon?: number
}

export interface CapNhatNguoiDungRequest {
  hoTen: string
  email?: string
  vaiTro: string
  maGiangVien?: number
  maBoMon?: number
  trangThai: string
}
