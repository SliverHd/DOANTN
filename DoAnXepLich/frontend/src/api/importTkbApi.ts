import axiosClient from './axiosClient'
import type { KetQuaPreviewImport, LichSuImport, DongThoiKhoaBieuImport } from '../types/importTkb'

export const importTkbApi = {
  preview: (file: File, maHocKy: number, maBoMon?: number) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('maHocKy', maHocKy.toString())
    if (maBoMon && maBoMon > 0) {
      formData.append('maBoMon', maBoMon.toString())
    }
    return axiosClient.post<KetQuaPreviewImport>('/import-thoi-khoa-bieu/preview', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  xacNhan: (maHocKy: number, tenFile: string, danhSachDong: DongThoiKhoaBieuImport[], maBoMon?: number) => {
    return axiosClient.post<{ soDongThanhCong: number }>('/import-thoi-khoa-bieu/xac-nhan', {
      maHocKy,
      maBoMon: maBoMon && maBoMon > 0 ? maBoMon : undefined,
      tenFile,
      danhSachDong,
    })
  },

  getLichSu: (maHocKy: number) => {
    return axiosClient.get<LichSuImport[]>(`/import-thoi-khoa-bieu/lich-su/${maHocKy}`)
  },

  xoaLichSu: (id: number) => {
    return axiosClient.delete(`/import-thoi-khoa-bieu/lich-su/${id}`)
  },

  taiFileMau: () =>
    axiosClient.get<Blob>('/import-thoi-khoa-bieu/file-mau', {
      responseType: 'blob',
    }),

  downloadFileMauUrl: '/api/import-thoi-khoa-bieu/file-mau',
}
