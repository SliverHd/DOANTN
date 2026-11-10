import axiosClient from './axiosClient'
import type {
  LichBan,
  NguyenVong,
  ThemNguyenVongRequest,
  LuuMaTranNguyenVongRequest,
  KetQuaDuyetNguyenVong,
  TaoLichSeminarRequest,
  KetQuaTaoLichSeminar,
  XoaNhomSeminarRequest,
} from '../types/nguyenVong'

export const nguyenVongApi = {
  getNguyenVong: (maHocKy: number, maGiangVien?: number, maBoMon?: number) =>
    axiosClient.get<NguyenVong[]>('/nguyen-vong', {
      params: {
        maHocKy,
        maGiangVien: maGiangVien && maGiangVien > 0 ? maGiangVien : undefined,
        maBoMon: maBoMon && maBoMon > 0 ? maBoMon : undefined,
      },
    }),

  themNguyenVong: (data: ThemNguyenVongRequest) =>
    axiosClient.post<NguyenVong>('/nguyen-vong', data),

  xoaNguyenVong: (id: number) =>
    axiosClient.delete<{ message: string }>(`/nguyen-vong/${id}`),

  duyet: (id: number, ghiChu?: string) =>
    axiosClient.post<KetQuaDuyetNguyenVong>(`/nguyen-vong/${id}/duyet`, { ghiChu }),

  tuChoi: (id: number, ghiChu?: string) =>
    axiosClient.post<KetQuaDuyetNguyenVong>(`/nguyen-vong/${id}/tu-choi`, { ghiChu }),

  getLichBan: (maHocKy: number, maGiangVien?: number, maBoMon?: number, chiSeminar?: boolean) =>
    axiosClient.get<LichBan[]>('/nguyen-vong/lich-ban', {
      params: {
        maHocKy,
        maGiangVien: maGiangVien && maGiangVien > 0 ? maGiangVien : undefined,
        maBoMon: maBoMon && maBoMon > 0 ? maBoMon : undefined,
        chiSeminar: chiSeminar !== undefined ? chiSeminar : undefined,
      },
    }),

  taoLichSeminar: (data: TaoLichSeminarRequest) =>
    axiosClient.post<KetQuaTaoLichSeminar>('/nguyen-vong/seminar', data),

  xoaLichBan: (id: number) =>
    axiosClient.delete<{ message: string }>(`/nguyen-vong/lich-ban/${id}`),

  xoaNhomSeminar: (data: XoaNhomSeminarRequest) =>
    axiosClient.post<{ soBanGhiDaXoa: number; message: string }>('/nguyen-vong/seminar/xoa-nhom', data),

  luuMaTran: (data: LuuMaTranNguyenVongRequest) =>
    axiosClient.post<{ message: string }>('/nguyen-vong/luu-ma-tran', data),
}
