import { useEffect, useState } from 'react'
import { hocKyApi } from '../../api/hocKyApi'
import { rangBuocApi } from '../../api/rangBuocApi'
import type { HocKy } from '../../types/hocKy'
import type { RangBuocHocKy, DinhMucGiangVien, KetQuaKiemTraRangBuoc } from '../../types/rangBuoc'
import { IconEdit } from '../../components/ActionIcons'

/** Chuẩn hóa chuỗi số nguyên không âm: cho phép rỗng khi đang xóa và loại bỏ số 0 dư ở đầu. */
function chuanHoaSoNguyen(rawValue: string): string {
  return rawValue.replace(/\D/g, '').replace(/^0+(?=\d)/, '')
}

function parseSoNguyenKhongAm(rawValue: string): number | null {
  const trimmed = rawValue.trim()
  if (!trimmed) return null
  const parsed = Number(trimmed)
  return Number.isInteger(parsed) && parsed >= 0 ? parsed : null
}

function DanhSachRangBuoc() {
  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [maHocKy, setMaHocKy] = useState<number>(0)
  const [tabHienTai, setTabHienTai] = useState<'rangBuoc' | 'dinhMuc' | 'kiemTra'>('rangBuoc')

  // Dữ liệu Ràng buộc
  const [danhSachRangBuoc, setDanhSachRangBuoc] = useState<RangBuocHocKy[]>([])
  const [dangSuaRbId, setDangSuaRbId] = useState<number | null>(null)
  const [trongSoTam, setTrongSoTam] = useState<string>('0')

  // Dữ liệu Định mức
  const [danhSachDinhMuc, setDanhSachDinhMuc] = useState<DinhMucGiangVien[]>([])
  const [dmChungToiThieu, setDmChungToiThieu] = useState<string>('6')
  const [dmChungToiDa, setDmChungToiDa] = useState<string>('25')

  // Modal sửa định mức từng GV
  const [gvDangSua, setGvDangSua] = useState<DinhMucGiangVien | null>(null)
  const [gvToiThieu, setGvToiThieu] = useState<string>('6')
  const [gvToiDa, setGvToiDa] = useState<string>('25')

  // Dữ liệu Kiểm tra ràng buộc cứng
  const [ketQuaKiemTra, setKetQuaKiemTra] = useState<KetQuaKiemTraRangBuoc | null>(null)
  const [dangKiemTra, setDangKiemTra] = useState(false)
  const [loiKiemTra, setLoiKiemTra] = useState('')

  const [dangTai, setDangTai] = useState(false)
  const [thongBao, setThongBao] = useState('')

  useEffect(() => {
    hocKyApi.getAll().then((res) => {
      setDanhSachHocKy(res.data)
      if (res.data.length > 0) {
        setMaHocKy(res.data[0].maHocKy)
      }
    })
  }, [])

  const taiDuLieu = async (hkId: number) => {
    if (!hkId) return
    try {
      setDangTai(true)
      const [resRb, resDm] = await Promise.all([
        rangBuocApi.getRangBuocByHocKy(hkId),
        rangBuocApi.getDinhMucByHocKy(hkId),
      ])
      setDanhSachRangBuoc(resRb.data)
      setDanhSachDinhMuc(resDm.data)
    } catch {
      // ignore
    } finally {
      setDangTai(false)
    }
  }

  useEffect(() => {
    if (maHocKy) {
      taiDuLieu(maHocKy)
      setKetQuaKiemTra(null)
      setLoiKiemTra('')
    }
  }, [maHocKy])

  // Lưu trọng số phạt
  const handleLuuTrongSo = async (id: number) => {
    const trongSo = parseSoNguyenKhongAm(trongSoTam)
    if (trongSo === null) {
      alert('Vui lòng nhập trọng số phạt là số nguyên không âm.')
      return
    }

    try {
      await rangBuocApi.capNhatTrongSo(id, trongSo)
      setDangSuaRbId(null)
      taiDuLieu(maHocKy)
      setThongBao('Cập nhật trọng số phạt thành công!')
      setTimeout(() => setThongBao(''), 2000)
    } catch {
      alert('Lỗi khi cập nhật trọng số.')
    }
  }

  // Áp dụng định mức chung
  const handleApDungChung = async () => {
    const soTietToiThieu = parseSoNguyenKhongAm(dmChungToiThieu)
    const soTietToiDa = parseSoNguyenKhongAm(dmChungToiDa)
    if (soTietToiThieu === null || soTietToiDa === null) {
      alert('Vui lòng nhập đầy đủ số tiết tối thiểu và tối đa hợp lệ.')
      return
    }
    if (soTietToiThieu > soTietToiDa) {
      alert('Số tiết tối thiểu không được lớn hơn số tiết tối đa.')
      return
    }

    try {
      await rangBuocApi.apDungDinhMucChung({
        maHocKy,
        soTietToiThieu,
        soTietToiDa,
      })
      taiDuLieu(maHocKy)
      setThongBao('Đã áp dụng định mức cho toàn bộ giảng viên!')
      setTimeout(() => setThongBao(''), 2000)
    } catch {
      alert('Lỗi khi áp dụng định mức chung.')
    }
  }

  // Lưu định mức từng GV
  const handleLuuDinhMucGv = async () => {
    if (!gvDangSua) return
    const soTietToiThieu = parseSoNguyenKhongAm(gvToiThieu)
    const soTietToiDa = parseSoNguyenKhongAm(gvToiDa)
    if (soTietToiThieu === null || soTietToiDa === null) {
      alert('Vui lòng nhập đầy đủ số tiết tối thiểu và tối đa hợp lệ.')
      return
    }
    if (soTietToiThieu > soTietToiDa) {
      alert('Số tiết tối thiểu không được lớn hơn số tiết tối đa.')
      return
    }

    try {
      await rangBuocApi.capNhatDinhMuc({
        maGiangVien: gvDangSua.maGiangVien,
        maHocKy,
        soTietToiThieu,
        soTietToiDa,
      })
      setGvDangSua(null)
      taiDuLieu(maHocKy)
      setThongBao('Cập nhật định mức giảng viên thành công!')
      setTimeout(() => setThongBao(''), 2000)
    } catch {
      alert('Lỗi khi cập nhật định mức.')
    }
  }

  // Chạy kiểm tra ràng buộc cứng
  const handleKiemTraRangBuoc = async () => {
    if (!maHocKy) {
      alert('Vui lòng chọn học kỳ.')
      return
    }

    try {
      setDangKiemTra(true)
      setLoiKiemTra('')
      const res = await rangBuocApi.kiemTraRangBuocCung(maHocKy)
      setKetQuaKiemTra(res.data)
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      setLoiKiemTra(typeof data === 'string' ? data : 'Lỗi khi kiểm tra ràng buộc cứng.')
    } finally {
      setDangKiemTra(false)
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h5 mb-1">Cấu hình Ràng buộc & Kiểm tra ràng buộc cứng</h2>
          <p className="text-muted small mb-0">
            Thiết lập trọng số phạt, định mức giảng viên và kiểm tra vi phạm ràng buộc cứng theo học kỳ
          </p>
        </div>

        <div className="d-flex align-items-center gap-2">
          <label className="form-label small fw-semibold mb-0">Học kỳ:</label>
          <select
            className="form-select form-select-sm"
            style={{ width: '180px' }}
            value={maHocKy}
            onChange={(e) => setMaHocKy(Number(e.target.value))}
          >
            {danhSachHocKy.map((hk) => (
              <option key={hk.maHocKy} value={hk.maHocKy}>
                {hk.tenHocKy}
              </option>
            ))}
          </select>
        </div>
      </div>

      {thongBao && <div className="alert alert-success py-2 small">{thongBao}</div>}

      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button
            className={`nav-link ${tabHienTai === 'rangBuoc' ? 'active fw-semibold' : ''}`}
            onClick={() => setTabHienTai('rangBuoc')}
          >
            1. Ràng buộc & Trọng số phạt ({danhSachRangBuoc.length})
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${tabHienTai === 'dinhMuc' ? 'active fw-semibold' : ''}`}
            onClick={() => setTabHienTai('dinhMuc')}
          >
            2. Định mức tiết giảng viên ({danhSachDinhMuc.length})
          </button>
        </li>
        <li className="nav-item">
          <button
            className={`nav-link ${tabHienTai === 'kiemTra' ? 'active fw-semibold' : ''}`}
            onClick={() => setTabHienTai('kiemTra')}
          >
            3. Kiểm tra ràng buộc cứng
          </button>
        </li>
      </ul>

      {tabHienTai === 'rangBuoc' && (
        <div className="table-responsive border bg-white">
          <table className="table table-modern table-hover table-sm mb-0">
            <thead>
              <tr>
                <th style={{ width: '60px' }}>STT</th>
                <th>Tên ràng buộc</th>
                <th style={{ width: '140px' }}>Phân loại</th>
                <th style={{ width: '180px' }}>Trọng số phạt</th>
                <th>Ý nghĩa nghiệp vụ</th>
                <th style={{ width: '110px' }} className="text-end">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {dangTai ? (
                <tr>
                  <td colSpan={6} className="text-center py-3 text-muted">
                    Đang tải danh sách ràng buộc...
                  </td>
                </tr>
              ) : (
                danhSachRangBuoc.map((rb, idx) => (
                  <tr key={rb.maRangBuoc}>
                    <td>{idx + 1}</td>
                    <td className="fw-semibold">{rb.tenRangBuoc}</td>
                    <td>
                      <span
                        className={`badge ${
                          rb.loaiRangBuoc === 'Cung'
                            ? 'bg-danger'
                            : 'bg-primary'
                        }`}
                      >
                        {rb.loaiRangBuoc === 'Cung' ? 'Ràng buộc cứng' : 'Ràng buộc mềm'}
                      </span>
                    </td>
                    <td>
                      {dangSuaRbId === rb.maRangBuoc ? (
                        <div className="d-flex align-items-center gap-1">
                          <input
                            type="text"
                            inputMode="numeric"
                            className="form-control form-control-sm"
                            style={{ width: '90px' }}
                            value={trongSoTam}
                            onChange={(e) => setTrongSoTam(chuanHoaSoNguyen(e.target.value))}
                          />
                          <button
                            className="btn btn-success btn-sm py-0 px-2"
                            onClick={() => handleLuuTrongSo(rb.maRangBuoc)}
                          >
                            Lưu
                          </button>
                          <button
                            className="btn btn-secondary btn-sm py-0 px-1"
                            onClick={() => setDangSuaRbId(null)}
                          >
                            X
                          </button>
                        </div>
                      ) : (
                        <span className="fw-bold">{rb.trongSoPhat}</span>
                      )}
                    </td>
                    <td className="text-muted small">{rb.moTa || '-'}</td>
                    <td className="text-end">
                      {dangSuaRbId !== rb.maRangBuoc && (
                        <div className="action-group justify-content-end">
                          <button
                            type="button"
                            className="btn-action btn-action--sm"
                            aria-label={`Sửa trọng số ${rb.tenRangBuoc}`}
                            onClick={() => {
                              setDangSuaRbId(rb.maRangBuoc)
                              setTrongSoTam(String(rb.trongSoPhat))
                            }}
                          >
                            <IconEdit />
                            <span>Sửa</span>
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {tabHienTai === 'dinhMuc' && (
        <div>
          {/* Form áp dụng nhanh định mức chung */}
          <div className="card border p-3 bg-light mb-3">
            <h3 className="h6 fw-bold mb-1">Thiết lập định mức giờ giảng chung theo tuần</h3>
            <p className="text-muted small mb-2">
              Đơn vị tính theo <strong>tiết/tuần</strong>. Quy chế mặc định: Cơ hữu <strong>6–25</strong>, Thỉnh giảng <strong>3–16</strong>, Trợ giảng <strong>0–12 tiết/tuần</strong>.
            </p>
            <div className="row g-2 align-items-end">
              <div className="col-md-3">
                <label className="form-label small fw-semibold">Số tiết tối thiểu / tuần</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="form-control form-control-sm"
                  value={dmChungToiThieu}
                  onChange={(e) => setDmChungToiThieu(chuanHoaSoNguyen(e.target.value))}
                  placeholder="VD: 6"
                />
              </div>
              <div className="col-md-3">
                <label className="form-label small fw-semibold">Số tiết tối đa / tuần</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="form-control form-control-sm"
                  value={dmChungToiDa}
                  onChange={(e) => setDmChungToiDa(chuanHoaSoNguyen(e.target.value))}
                  placeholder="VD: 25"
                />
              </div>
              <div className="col-md-4">
                <button className="btn btn-primary btn-sm" onClick={handleApDungChung}>
                  Áp dụng cho tất cả giảng viên
                </button>
              </div>
            </div>
          </div>

          <div className="table-responsive border bg-white">
            <table className="table table-modern table-hover table-sm mb-0">
              <thead>
                <tr>
                  <th style={{ width: '60px' }}>STT</th>
                  <th>Họ và tên giảng viên</th>
                  <th>Email</th>
                  <th>Số tiết tối thiểu / tuần</th>
                  <th>Số tiết tối đa / tuần</th>
                  <th style={{ width: '100px' }} className="text-end">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {dangTai ? (
                  <tr>
                    <td colSpan={6} className="text-center py-3 text-muted">
                      Đang tải danh sách định mức...
                    </td>
                  </tr>
                ) : danhSachDinhMuc.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="text-center py-3 text-muted">
                      Chưa có giảng viên nào trong hệ thống.
                    </td>
                  </tr>
                ) : (
                  danhSachDinhMuc.map((dm, idx) => (
                    <tr key={dm.maGiangVien}>
                      <td>{idx + 1}</td>
                      <td className="fw-semibold">{dm.tenGiangVien}</td>
                      <td className="text-muted">{dm.email}</td>
                      <td>
                        <span className="badge bg-secondary">{dm.soTietToiThieu} tiết/tuần</span>
                      </td>
                      <td>
                        <span className="badge bg-primary">{dm.soTietToiDa} tiết/tuần</span>
                      </td>
                      <td className="text-end">
                        <div className="action-group justify-content-end">
                          <button
                            type="button"
                            className="btn-action btn-action--sm"
                            aria-label={`Sửa định mức ${dm.tenGiangVien}`}
                            onClick={() => {
                              setGvDangSua(dm)
                              setGvToiThieu(String(dm.soTietToiThieu))
                              setGvToiDa(String(dm.soTietToiDa))
                            }}
                          >
                            <IconEdit />
                            <span>Sửa</span>
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Modal sửa định mức từng giảng viên */}
          {gvDangSua && (
            <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
              <div className="modal-dialog modal-sm">
                <div className="modal-content">
                  <div className="modal-header py-2">
                    <h5 className="modal-title h6">Sửa định mức giảng viên</h5>
                    <button
                      type="button"
                      className="btn-close"
                      onClick={() => setGvDangSua(null)}
                    />
                  </div>
                  <div className="modal-body">
                    <p className="small mb-2 fw-semibold">{gvDangSua.tenGiangVien}</p>

                    <div className="mb-2">
                      <label className="form-label small fw-semibold">Số tiết tối thiểu / tuần</label>
                      <input
                        type="text"
                        inputMode="numeric"
                        className="form-control form-control-sm"
                        value={gvToiThieu}
                        onChange={(e) => setGvToiThieu(chuanHoaSoNguyen(e.target.value))}
                        placeholder="VD: 6"
                      />
                    </div>
                    <div className="mb-2">
                      <label className="form-label small fw-semibold">Số tiết tối đa / tuần</label>
                      <input
                        type="text"
                        inputMode="numeric"
                        className="form-control form-control-sm"
                        value={gvToiDa}
                        onChange={(e) => setGvToiDa(chuanHoaSoNguyen(e.target.value))}
                        placeholder="VD: 25"
                      />
                    </div>
                    <div className="text-muted" style={{ fontSize: '12px' }}>
                      Quy chế: Cơ hữu 6–25, Thỉnh giảng 3–16, Trợ giảng 0–12 tiết/tuần.
                    </div>
                  </div>
                  <div className="modal-footer py-2">
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      onClick={() => setGvDangSua(null)}
                    >
                      Hủy
                    </button>
                    <button
                      type="button"
                      className="btn btn-primary btn-sm"
                      onClick={handleLuuDinhMucGv}
                    >
                      Lưu
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {tabHienTai === 'kiemTra' && (
        <div className="bg-white border p-3">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <div>
              <h3 className="h6 fw-bold mb-1">Bộ kiểm tra ràng buộc cứng (Hard Constraints Checker)</h3>
              <p className="text-muted small mb-0">
                Kiểm tra 4 ràng buộc bắt buộc: Phủ kín lớp, đúng chuyên môn, không trùng lịch và tránh lịch bận
              </p>
            </div>
            <button
              className="btn btn-primary btn-sm"
              onClick={handleKiemTraRangBuoc}
              disabled={dangKiemTra || !maHocKy}
            >
              {dangKiemTra ? 'Đang kiểm tra...' : 'Kiểm tra vi phạm ngay'}
            </button>
          </div>

          {loiKiemTra && <div className="alert alert-danger py-2 small">{loiKiemTra}</div>}

          {ketQuaKiemTra ? (
            <div>
              {/* Thống kê chỉ số */}
              <div className="row g-2 mb-3">
                <div className="col-md-2">
                  <div className="border p-2 text-center bg-light">
                    <div className="small text-muted">Tổng số lớp</div>
                    <div className="h5 mb-0 fw-bold">{ketQuaKiemTra.tongSoLop}</div>
                  </div>
                </div>
                <div className="col-md-2">
                  <div className="border p-2 text-center bg-light">
                    <div className="small text-muted">Đã phân công</div>
                    <div className="h5 mb-0 fw-bold text-success">{ketQuaKiemTra.soLopDaPhanCong}</div>
                  </div>
                </div>
                <div className="col-md-2">
                  <div className="border p-2 text-center bg-light">
                    <div className="small text-muted">Chưa phân công</div>
                    <div className="h5 mb-0 fw-bold text-danger">{ketQuaKiemTra.soLopChuaPhanCong}</div>
                  </div>
                </div>
                <div className="col-md-2">
                  <div className="border p-2 text-center bg-light">
                    <div className="small text-muted">Sai chuyên môn</div>
                    <div className="h5 mb-0 fw-bold text-danger">{ketQuaKiemTra.soViPhamChuyenMon}</div>
                  </div>
                </div>
                <div className="col-md-2">
                  <div className="border p-2 text-center bg-light">
                    <div className="small text-muted">Trùng lịch dạy</div>
                    <div className="h5 mb-0 fw-bold text-danger">{ketQuaKiemTra.soViPhamTrungLich}</div>
                  </div>
                </div>
                <div className="col-md-2">
                  <div className="border p-2 text-center bg-light">
                    <div className="small text-muted">Vướng lịch bận</div>
                    <div className="h5 mb-0 fw-bold text-danger">{ketQuaKiemTra.soViPhamLichBan}</div>
                  </div>
                </div>
              </div>

              {ketQuaKiemTra.hopLe ? (
                <div className="alert alert-success py-2 small mb-0">
                  Phương án phân công hiện tại đạt 100% ràng buộc cứng: Tất cả các lớp đã có giảng viên, đúng chuyên môn, không trùng lịch và không vướng lịch bận cá nhân!
                </div>
              ) : (
                <div>
                  <div className="alert alert-danger py-2 small mb-2">
                    Phát hiện {ketQuaKiemTra.danhSachViPham.length} vi phạm ràng buộc cứng cần xử lý:
                  </div>
                  <div className="table-responsive border">
                    <table className="table table-sm table-hover mb-0" style={{ fontSize: '0.85rem' }}>
                      <thead className="table-light">
                        <tr>
                          <th style={{ width: '60px' }}>STT</th>
                          <th>Nội dung chi tiết vi phạm</th>
                        </tr>
                      </thead>
                      <tbody>
                        {ketQuaKiemTra.danhSachViPham.map((vp, idx) => (
                          <tr key={idx} className="table-danger">
                            <td>{idx + 1}</td>
                            <td>{vp}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="text-center py-4 text-muted small">
              Nhấn nút &quot;Kiểm tra vi phạm ngay&quot; để quét toàn bộ dữ liệu phân công trong học kỳ.
            </div>
          )}
        </div>
      )}
    </section>
  )
}

export default DanhSachRangBuoc
