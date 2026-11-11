import axiosClient from './axiosClient'
import type { HocPhan, TaoHocPhanDto } from '../types/hocPhan'

export const hocPhanApi = {
  getAll: (maBoMon?: number, maHocKy?: number) =>
    axiosClient.get<HocPhan[]>('/hoc-phan', {
      params: {
        maBoMon: maBoMon && maBoMon > 0 ? maBoMon : undefined,
        maHocKy: maHocKy && maHocKy > 0 ? maHocKy : undefined,
      },
    }),
  getById: (id: number) => axiosClient.get<HocPhan>(`/hoc-phan/${id}`),
  create: (data: TaoHocPhanDto) => axiosClient.post<HocPhan>('/hoc-phan', data),
  update: (id: number, data: TaoHocPhanDto) => axiosClient.put(`/hoc-phan/${id}`, data),
  delete: (id: number) => axiosClient.delete(`/hoc-phan/${id}`),
}
