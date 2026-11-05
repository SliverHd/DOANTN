import axiosClient from './axiosClient'
import type { GiangVien, TaoGiangVienDto, NangLucGiangDay } from '../types/giangVien'

export const giangVienApi = {
  getAll: (maBoMon?: number) =>
    axiosClient.get<GiangVien[]>('/giang-vien', { params: maBoMon ? { maBoMon } : {} }),
  getById: (id: number) => axiosClient.get<GiangVien>(`/giang-vien/${id}`),
  create: (data: TaoGiangVienDto) => axiosClient.post<GiangVien>('/giang-vien', data),
  update: (id: number, data: TaoGiangVienDto) => axiosClient.put(`/giang-vien/${id}`, data),
  delete: (id: number) => axiosClient.delete(`/giang-vien/${id}`),
  getNangLuc: (id: number) => axiosClient.get<NangLucGiangDay[]>(`/giang-vien/${id}/nang-luc`),
  capNhatNangLuc: (id: number, danhSachMaHocPhan: number[]) =>
    axiosClient.put(`/giang-vien/${id}/nang-luc`, { danhSachMaHocPhan }),
}
