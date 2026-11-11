import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { hocKyApi } from '../../api/hocKyApi'
import { boMonApi } from '../../api/boMonApi'
import { xepLichApi } from '../../api/xepLichApi'
import { useAuth } from '../../contexts/AuthContext'
import type { HocKy } from '../../types/hocKy'
import type { BoMon } from '../../types/boMon'
import type { KetQuaXepLich, YeuCauXepLich } from '../../types/xepLich'
import DoThiTienHoaGA from '../../components/DoThiTienHoaGA'

function ChayXepLich() {
  const { user } = useAuth()
  const [danhSachHocKy, setDanhSachHocKy] = useState<HocKy[]>([])
  const [danhSachBoMon, setDanhSachBoMon] = useState<BoMon[]>([])
  const [form, setForm] = useState<YeuCauXepLich>({
    maHocKy: 0,
    maBoMon: user?.maBoMon || 0,
    thuatToan: 'GA',
    kichThuocQuanThe: 80,
    soTheHe: 350,
    tyLeLaiGhep: 0.85,
    tyLeDotBien: 0.08,
    thoiGianChayToiDa: 30,
    soLuongWorker: 1,
  })
  const [ketQua, setKetQua] = useState<KetQuaXepLich | null>(null)
  const [dangChay, setDangChay] = useState(false)
  const [loi, setLoi] = useState('')

  useEffect(() => {
    hocKyApi.getAll().then((res) => {
      setDanhSachHocKy(res.data)
      if (res.data.length > 0) {
        setForm((prev) => ({ ...prev, maHocKy: res.data[0].maHocKy }))
      }
    })

    boMonApi.getAll().then((res) => {
      setDanhSachBoMon(res.data)
      if (user?.maBoMon) {
        setForm((prev) => ({ ...prev, maBoMon: user.maBoMon }))
      }
    })
  }, [])

  const capNhatSo = (name: keyof YeuCauXepLich, value: string) => {
    setForm((current) => ({
      ...current,
      [name]: value === '' ? undefined : Number(value),
    }))
  }

  const chayXepLich = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!form.maHocKy || form.maHocKy <= 0) {
      alert('Vui lòng chọn học kỳ.')
      return
    }

    setDangChay(true)
    setLoi('')
    setKetQua(null)

    const payload =
      form.thuatToan === 'CP_SAT'
        ? {
          maHocKy: form.maHocKy,
          maBoMon: form.maBoMon && form.maBoMon > 0 ? form.maBoMon : undefined,
          thuatToan: form.thuatToan,
          thoiGianChayToiDa: form.thoiGianChayToiDa ?? 30,
          soLuongWorker: form.soLuongWorker ?? 1,
        }
        : {
          ...form,
          maBoMon: form.maBoMon && form.maBoMon > 0 ? form.maBoMon : undefined,
        }

    try {
      const response = await xepLichApi.chayXepLich(payload)
      setKetQua(response.data)
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: unknown } }
      const data = errorObj.response?.data
      if (typeof data === 'string') {
        setLoi(data)
      } else if (data && typeof data === 'object') {
        const obj = data as { detail?: string; thongDiep?: string; title?: string }
        setLoi(obj.detail || obj.thongDiep || obj.title || 'Lỗi khi chạy thuật toán xếp lịch.')
      } else {
        setLoi('Lỗi khi chạy thuật toán xếp lịch.')
      }
    } finally {
      setDangChay(false)
    }
  }

  return (
    <section>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <div>
          <h2 className="h5 mb-1">Chạy thuật toán xếp lịch tự động</h2>
          <p className="text-muted small mb-0">
            Hỗ trợ giải thuật di truyền (GA) và bộ giải ràng buộc tối ưu CP-SAT (Google OR-Tools)
          </p>
        </div>
      </div>

      <div className="card p-3 mb-4">
        <form className="row g-3" onSubmit={chayXepLich}>
          <div className="col-md-4">
            <label className="form-label small fw-semibold" htmlFor="maHocKy">
              Học kỳ áp dụng
            </label>
            <select
              id="maHocKy"
              className="form-select form-select-sm"
              value={form.maHocKy}
              onChange={(e) => setForm((prev) => ({ ...prev, maHocKy: Number(e.target.value) }))}
            >
              {danhSachHocKy.map((hk) => (
                <option key={hk.maHocKy} value={hk.maHocKy}>
                  {hk.tenHocKy}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-4">
            <label className="form-label small fw-semibold" htmlFor="maBoMon">
              Bộ môn xếp lịch
            </label>
            <select
              id="maBoMon"
              className="form-select form-select-sm"
              value={form.maBoMon || 0}
              onChange={(e) => setForm((prev) => ({ ...prev, maBoMon: Number(e.target.value) }))}
            >
              <option value={0}>-- Tất cả bộ môn --</option>
              {danhSachBoMon.map((bm) => (
                <option key={bm.maBoMon} value={bm.maBoMon}>
                  {bm.tenBoMon}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-4">
            <label className="form-label small fw-semibold" htmlFor="thuatToan">
              Thuật toán xếp lịch
            </label>
            <select
              id="thuatToan"
              className="form-select form-select-sm"
              value={form.thuatToan}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  thuatToan: event.target.value as YeuCauXepLich['thuatToan'],
                }))
              }
            >
              <option value="GA">Genetic Algorithm (GA - Giải thuật di truyền)</option>
              <option value="CP_SAT">CP-SAT (Google OR-Tools - Bộ giải ràng buộc)</option>
            </select>
          </div>

          {form.thuatToan === 'GA' && (
            <>
              <div className="col-md-3">
                <label className="form-label small fw-semibold" htmlFor="kichThuocQuanThe">
                  Kích thước quần thể (Pop Size)
                </label>
                <input
                  id="kichThuocQuanThe"
                  type="number"
                  min={10}
                  max={500}
                  className="form-control form-control-sm"
                  value={form.kichThuocQuanThe ?? ''}
                  onChange={(event) => capNhatSo('kichThuocQuanThe', event.target.value)}
                />
              </div>

              <div className="col-md-3">
                <label className="form-label small fw-semibold" htmlFor="soTheHe">
                  Số thế hệ tối đa
                </label>
                <input
                  id="soTheHe"
                  type="number"
                  min={10}
                  max={1000}
                  className="form-control form-control-sm"
                  value={form.soTheHe ?? ''}
                  onChange={(event) => capNhatSo('soTheHe', event.target.value)}
                />
              </div>

              <div className="col-md-3">
                <label className="form-label small fw-semibold" htmlFor="tyLeLaiGhep">
                  Tỷ lệ lai ghép
                </label>
                <input
                  id="tyLeLaiGhep"
                  type="number"
                  min={0.1}
                  max={1}
                  step={0.05}
                  className="form-control form-control-sm"
                  value={form.tyLeLaiGhep ?? ''}
                  onChange={(event) => capNhatSo('tyLeLaiGhep', event.target.value)}
                />
              </div>

              <div className="col-md-3">
                <label className="form-label small fw-semibold" htmlFor="tyLeDotBien">
                  Tỷ lệ đột biến
                </label>
                <input
                  id="tyLeDotBien"
                  type="number"
                  min={0.01}
                  max={0.5}
                  step={0.01}
                  className="form-control form-control-sm"
                  value={form.tyLeDotBien ?? ''}
                  onChange={(event) => capNhatSo('tyLeDotBien', event.target.value)}
                />
              </div>
            </>
          )}

          {form.thuatToan === 'CP_SAT' && (
            <>
              <div className="col-md-6">
                <label className="form-label small fw-semibold" htmlFor="thoiGianChayToiDa">
                  Giới hạn thời gian (giây)
                </label>
                <input
                  id="thoiGianChayToiDa"
                  type="number"
                  min={1}
                  max={300}
                  className="form-control form-control-sm"
                  value={form.thoiGianChayToiDa ?? 30}
                  onChange={(event) => capNhatSo('thoiGianChayToiDa', event.target.value)}
                />
              </div>

              <div className="col-md-6">
                <label className="form-label small fw-semibold" htmlFor="soLuongWorker">
                  Số luồng CPU (Workers)
                </label>
                <input
                  id="soLuongWorker"
                  type="number"
                  min={1}
                  max={32}
                  className="form-control form-control-sm"
                  value={form.soLuongWorker ?? 1}
                  onChange={(event) => capNhatSo('soLuongWorker', event.target.value)}
                />
              </div>
            </>
          )}

          <div className="col-12 mt-3">
            <button type="submit" className="btn btn-primary btn-sm" disabled={dangChay}>
              {dangChay ? 'Đang chạy thuật toán...' : 'Bắt đầu xếp lịch'}
            </button>
          </div>
        </form>
      </div>

      {loi && <div className="alert alert-danger py-2 small">{loi}</div>}

      {ketQua && (
        <div className="card border p-3 bg-white">
          <div className="d-flex justify-content-between align-items-center mb-3">
            <h3 className="h6 fw-bold mb-0">Kết quả chạy thuật toán {ketQua.thuatToan}</h3>
            <span
              className={`badge ${(ketQua.soViPhamCung ?? 0) === 0
                  ? 'bg-success'
                  : ketQua.trangThai === 'KhaThiMotPhan'
                    ? 'bg-warning text-dark'
                    : 'bg-danger'
                }`}
            >
              {(ketQua.soViPhamCung ?? 0) === 0
                ? 'HỢP LỆ (Thỏa mãn ràng buộc cứng)'
                : ketQua.trangThai === 'KhaThiMotPhan'
                  ? `KHẢ THI MỘT PHẦN (${ketQua.soLopChuaPhanCong ?? ketQua.soViPhamCung} lớp bỏ trống)`
                  : `CÒN VI PHẠM (${ketQua.soViPhamCung})`}
            </span>
          </div>

          {ketQua.thongDiep && (
            <div className={`alert py-2 small mb-3 ${(ketQua.soViPhamCung ?? 0) === 0 ? 'alert-success' : ketQua.trangThai === 'KhaThiMotPhan' ? 'alert-warning' : 'alert-danger'}`}>
              {ketQua.thongDiep}
            </div>
          )}

          <div className="row g-2 mb-3 text-center">
            <div className="col-md-3">
              <div className="p-2 border rounded bg-light">
                <div className="small text-muted">Thời gian chạy</div>
                <div className="fw-bold">{ketQua.thoiGianChayMiligiay ?? (ketQua.thoiGianChayGiay ? ketQua.thoiGianChayGiay * 1000 : 0)} ms</div>
              </div>
            </div>
            <div className="col-md-3">
              <div className="p-2 border rounded bg-light">
                <div className="small text-muted">Trạng thái</div>
                <div className="fw-bold text-primary">{ketQua.trangThai}</div>
              </div>
            </div>
            <div className="col-md-3">
              <div className="p-2 border rounded bg-light">
                <div className="small text-muted">Tổng điểm phạt</div>
                <div className="fw-bold text-danger">{ketQua.tongDiemPhat.toFixed(1)}</div>
              </div>
            </div>
            <div className="col-md-3">
              <div className="p-2 border rounded bg-light">
                <div className="small text-muted">Số lớp đã phân công</div>
                <div className="fw-bold text-success">{ketQua.danhSachPhanCong?.length ?? 0}</div>
              </div>
            </div>
          </div>

          {ketQua.danhSachViPham && ketQua.danhSachViPham.length > 0 && (
            <div className="alert alert-warning py-2 mb-3">
              <div className="fw-semibold small mb-1">Danh sách cảnh báo vi phạm:</div>
              <ul className="mb-0 ps-3 small">
                {ketQua.danhSachViPham.map((vp, i) => (
                  <li key={i}>{vp}</li>
                ))}
              </ul>
            </div>
          )}

          {ketQua.chiTietViPham && ketQua.chiTietViPham.length > 0 && (
            <div className="alert alert-warning py-2 mb-3">
              <div className="fw-semibold small mb-1">Cảnh báo vi phạm ràng buộc mềm:</div>
              <ul className="mb-0 ps-3 small">
                {ketQua.chiTietViPham.map((vp, idx) => (
                  <li key={idx}>{vp}</li>
                ))}
              </ul>
            </div>
          )}

          {ketQua.lichSuDiemPhatTheoTheHe && ketQua.lichSuDiemPhatTheoTheHe.length > 0 && (
            <div className="mb-3">
              <DoThiTienHoaGA
                duLieu={ketQua.lichSuDiemPhatTheoTheHe}
                soTheHe={ketQua.soTheHeThucTe ?? form.soTheHe}
                thuatToan={ketQua.thuatToan}
              />
            </div>
          )}

          <div className="d-flex gap-2">
            <Link to="/ket-qua" className="btn btn-success btn-sm">
              Xem bảng phân công chi tiết & Thống kê tải &rarr;
            </Link>
          </div>
        </div>
      )}
    </section>
  )
}

export default ChayXepLich
