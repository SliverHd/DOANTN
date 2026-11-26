import axiosClient from './axiosClient'
import type { LopHocPhan } from '../types/lopHocPhan'

export const thoiKhoaBieuApi = {
  getAll: (maHocKy?: number, maBoMon?: number) =>
    axiosClient.get<LopHocPhan[]>('/thoi-khoa-bieu', {
      params: {
        maHocKy: maHocKy && maHocKy > 0 ? maHocKy : undefined,
        maBoMon: maBoMon && maBoMon > 0 ? maBoMon : undefined,
      },
    }),
  delete: (id: number) => axiosClient.delete(`/thoi-khoa-bieu/${id}`),
  deleteAll: (maHocKy: number, maBoMon?: number) =>
    axiosClient.delete<{ soLopDaXoa: number; soPhanCongDaXoa: number; message: string }>(
      '/thoi-khoa-bieu/xoa-toan-bo',
      {
        params: {
          maHocKy,
          maBoMon: maBoMon && maBoMon > 0 ? maBoMon : undefined,
        },
      }
    ),
}
