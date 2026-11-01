import React, { useState, useRef } from 'react'

interface DoThiTienHoaGAProps {
  duLieu: number[]
  soTheHe?: number
  thuatToan?: string
}

export const DoThiTienHoaGA: React.FC<DoThiTienHoaGAProps> = ({
  duLieu,
  soTheHe,
  thuatToan = 'GA',
}) => {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null)
  const [dangXuatAnh, setDangXuatAnh] = useState(false)
  const svgRef = useRef<SVGSVGElement | null>(null)

  if (!duLieu || duLieu.length === 0) {
    return null
  }

  // Kích thước SVG
  const width = 860
  const height = 430
  const padding = { top: 45, right: 35, bottom: 65, left: 65 }
  const plotWidth = width - padding.left - padding.right
  const plotHeight = height - padding.top - padding.bottom

  // Tính toán thang đo trục Y
  const rawMaxY = Math.max(...duLieu, 1)
  // Làm tròn trần lên mốc nguyên đẹp (ví dụ max 3.8 -> 4 hoặc 5, hoặc tối thiểu 5 như mẫu)
  const maxY = Math.max(Math.ceil(rawMaxY * 1.15), 5)
  const minY = 0

  // Tính toán thang đo trục X (tối thiểu 100 hoặc mốc chia tròn chục 50)
  const tongSoDiem = duLieu.length
  const maxXConfig = Math.max(tongSoDiem - 1, soTheHe ?? 100, 100)
  // Làm tròn maxX lên bội số của 50 (ví dụ 160 -> 200 như ảnh mẫu)
  const maxX = Math.ceil(maxXConfig / 50) * 50

  // Hàm chuyển đổi tọa độ
  const getX = (index: number) => {
    return padding.left + (index / maxX) * plotWidth
  }

  const getY = (val: number) => {
    const clamped = Math.max(minY, Math.min(maxY, val))
    return padding.top + plotHeight - ((clamped - minY) / (maxY - minY)) * plotHeight
  }

  // Xây dựng đường gấp khúc SVG polyline
  const points = duLieu.map((val, idx) => `${getX(idx).toFixed(1)},${getY(val).toFixed(1)}`).join(' ')

  // Tạo các mốc chia trục Y (mỗi nấc 1 đơn vị, tối đa 8 vạch)
  const yStep = maxY <= 6 ? 1 : Math.ceil(maxY / 6)
  const yTicks: number[] = []
  for (let y = 0; y <= maxY; y += yStep) {
    yTicks.push(y)
  }

  // Tạo các mốc chia trục X (bước nhảy 50 như ảnh mẫu: 0, 50, 100, 150, 200...)
  const xStep = maxX <= 200 ? 50 : Math.ceil(maxX / 5 / 50) * 50
  const xTicks: number[] = []
  for (let x = 0; x <= maxX; x += xStep) {
    xTicks.push(x)
  }

  // Xử lý rê chuột xem giá trị
  const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
    if (!svgRef.current) return
    const rect = svgRef.current.getBoundingClientRect()
    const mouseX = e.clientX - rect.left
    const svgMouseX = (mouseX / rect.width) * width

    if (svgMouseX < padding.left || svgMouseX > padding.left + plotWidth) {
      setHoverIndex(null)
      return
    }

    const relX = svgMouseX - padding.left
    const idxCalc = Math.round((relX / plotWidth) * maxX)
    if (idxCalc >= 0 && idxCalc < tongSoDiem) {
      setHoverIndex(idxCalc)
    } else {
      setHoverIndex(null)
    }
  }

  // Xuất ảnh đồ thị ra PNG độ phân giải cao
  const handleXuatAnh = () => {
    if (!svgRef.current) return
    setDangXuatAnh(true)
    try {
      const svgElement = svgRef.current
      const svgString = new XMLSerializer().serializeToString(svgElement)
      const svgBlob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' })
      const URL = window.URL || window.webkitURL || window
      const blobURL = URL.createObjectURL(svgBlob)

      const image = new Image()
      image.onload = () => {
        const canvas = document.createElement('canvas')
        canvas.width = width * 2 // 2x Retina resolution
        canvas.height = height * 2
        const ctx = canvas.getContext('2d')
        if (ctx) {
          ctx.scale(2, 2)
          ctx.drawImage(image, 0, 0)
          const pngUrl = canvas.toDataURL('image/png')
          const downloadLink = document.createElement('a')
          downloadLink.href = pngUrl
          downloadLink.download = `DoThi_TienHoa_GA_${new Date().toISOString().slice(0, 10)}.png`
          document.body.appendChild(downloadLink)
          downloadLink.click()
          document.body.removeChild(downloadLink)
        }
        URL.revokeObjectURL(blobURL)
        setDangXuatAnh(false)
      }
      image.src = blobURL
    } catch (err) {
      console.error('Lỗi xuất ảnh đồ thị:', err)
      setDangXuatAnh(false)
    }
  }

  const diemDau = duLieu[0] ?? 0
  const diemTotNhat = Math.min(...duLieu)
  const tyLeCaiThien = diemDau > 0 ? Math.max(0, Math.round(((diemDau - diemTotNhat) / diemDau) * 100)) : 100

  return (
    <div className="card shadow-sm border mt-3" style={{ borderRadius: '6px', overflow: 'hidden' }}>
      {/* Thanh Header mô phỏng đúng giao diện tab trong ảnh mẫu */}
      <div
        className="d-flex justify-content-between align-items-center px-3 py-2"
        style={{
          background: 'linear-gradient(to bottom, #f1f5f9, #e2e8f0)',
          borderBottom: '1px solid #cbd5e1',
        }}
      >
        <div className="d-flex align-items-center gap-1">
          <button
            type="button"
            className="btn btn-sm btn-white border fw-bold text-primary shadow-xs px-3"
            style={{
              backgroundColor: '#ffffff',
              borderColor: '#94a3b8',
              borderBottom: '2px solid #2563eb',
              fontSize: '13px',
            }}
          >
            📈 Đồ thị {thuatToan ? `(${thuatToan})` : ''}
          </button>
        </div>

        <div className="d-flex align-items-center gap-2">
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary py-1 px-2"
            style={{ fontSize: '12px' }}
            onClick={handleXuatAnh}
            disabled={dangXuatAnh}
            title="Lưu ảnh đồ thị độ phân giải cao để chèn vào báo cáo đồ án"
          >
            {dangXuatAnh ? 'Đang tạo ảnh...' : '📷 Tải ảnh PNG'}
          </button>
        </div>
      </div>

      <div className="p-3 bg-white">
        {/* Khung đồ thị vẽ theo phong cách mẫu ảnh */}
        <div
          className="position-relative text-center mx-auto"
          style={{ maxWidth: '100%', overflowX: 'auto' }}
        >
          <svg
            ref={svgRef}
            viewBox={`0 0 ${width} ${height}`}
            style={{
              width: '100%',
              maxWidth: `${width}px`,
              height: 'auto',
              display: 'block',
              margin: '0 auto',
              userSelect: 'none',
              cursor: 'crosshair',
            }}
            onMouseMove={handleMouseMove}
            onMouseLeave={() => setHoverIndex(null)}
          >
            {/* Tiêu đề đồ thị nằm chính giữa như ảnh mẫu */}
            <text
              x={width / 2}
              y={28}
              textAnchor="middle"
              style={{
                fontFamily: 'system-ui, -apple-system, sans-serif',
                fontSize: '17px',
                fontWeight: 'bold',
                fill: '#0f172a',
              }}
            >
              Đồ thị biểu diễn
            </text>

            {/* Vùng hộp tọa độ (màu vàng nhạt đặc trưng của phần mềm mẫu) */}
            <rect
              x={padding.left}
              y={padding.top}
              width={plotWidth}
              height={plotHeight}
              fill="#fffde7"
              stroke="#1e293b"
              strokeWidth="1.2"
            />

            {/* Lưới tọa độ ngang mờ và nhãn trục Y */}
            {yTicks.map((yVal) => {
              const yPos = getY(yVal)
              return (
                <g key={`y-${yVal}`}>
                  <line
                    x1={padding.left}
                    y1={yPos}
                    x2={padding.left + plotWidth}
                    y2={yPos}
                    stroke="#e2dfb8"
                    strokeWidth="0.8"
                    strokeDasharray="3,3"
                  />
                  {/* Vạch chia trục Y */}
                  <line
                    x1={padding.left - 5}
                    y1={yPos}
                    x2={padding.left}
                    y2={yPos}
                    stroke="#1e293b"
                    strokeWidth="1.2"
                  />
                  {/* Số trục Y */}
                  <text
                    x={padding.left - 9}
                    y={yPos + 4}
                    textAnchor="end"
                    style={{
                      fontFamily: 'Arial, sans-serif',
                      fontSize: '12px',
                      fill: '#1e293b',
                    }}
                  >
                    {yVal}
                  </text>
                </g>
              )
            })}

            {/* Lưới tọa độ dọc mờ và nhãn trục X */}
            {xTicks.map((xVal) => {
              const xPos = getX(xVal)
              return (
                <g key={`x-${xVal}`}>
                  <line
                    x1={xPos}
                    y1={padding.top}
                    x2={xPos}
                    y2={padding.top + plotHeight}
                    stroke="#e2dfb8"
                    strokeWidth="0.8"
                    strokeDasharray="3,3"
                  />
                  {/* Vạch chia trục X */}
                  <line
                    x1={xPos}
                    y1={padding.top + plotHeight}
                    x2={xPos}
                    y2={padding.top + plotHeight + 5}
                    stroke="#1e293b"
                    strokeWidth="1.2"
                  />
                  {/* Số trục X */}
                  <text
                    x={xPos}
                    y={padding.top + plotHeight + 18}
                    textAnchor="middle"
                    style={{
                      fontFamily: 'Arial, sans-serif',
                      fontSize: '12px',
                      fill: '#1e293b',
                    }}
                  >
                    {xVal}
                  </text>
                </g>
              )
            })}

            {/* Nhãn trục tung (Y) xoay dọc: "Giá trị phạt" */}
            <text
              transform={`rotate(-90)`}
              x={-(padding.top + plotHeight / 2)}
              y={20}
              textAnchor="middle"
              style={{
                fontFamily: 'system-ui, -apple-system, sans-serif',
                fontSize: '14px',
                fontWeight: 'bold',
                fill: '#0f172a',
              }}
            >
              Giá trị phạt
            </text>

            {/* Nhãn trục hoành (X): "Cá thể" */}
            <text
              x={padding.left + plotWidth / 2}
              y={height - 18}
              textAnchor="middle"
              style={{
                fontFamily: 'system-ui, -apple-system, sans-serif',
                fontSize: '14px',
                fontWeight: 'bold',
                fill: '#0f172a',
              }}
            >
              Cá thể
            </text>

            {/* Đường vẽ đồ thị biểu diễn hàm phạt (Màu đỏ mận theo ảnh mẫu) */}
            <polyline
              fill="none"
              stroke="#b91c1c"
              strokeWidth="1.5"
              strokeLinejoin="round"
              strokeLinecap="round"
              points={points}
            />

            {/* Điểm nhấn & thanh định vị khi hover */}
            {hoverIndex !== null && hoverIndex < tongSoDiem && (
              <g>
                {/* Đường dóng dọc */}
                <line
                  x1={getX(hoverIndex)}
                  y1={padding.top}
                  x2={getX(hoverIndex)}
                  y2={padding.top + plotHeight}
                  stroke="#3b82f6"
                  strokeWidth="1"
                  strokeDasharray="4,2"
                />
                {/* Chấm tròn giá trị */}
                <circle
                  cx={getX(hoverIndex)}
                  cy={getY(duLieu[hoverIndex])}
                  r="4.5"
                  fill="#ffffff"
                  stroke="#b91c1c"
                  strokeWidth="2.5"
                />
                {/* Hộp Tooltip nổi bật */}
                <g
                  transform={`translate(${Math.min(
                    Math.max(getX(hoverIndex) - 60, padding.left + 5),
                    padding.left + plotWidth - 125
                  )}, ${Math.max(getY(duLieu[hoverIndex]) - 48, padding.top + 5)})`}
                >
                  <rect
                    width="120"
                    height="40"
                    rx="4"
                    fill="rgba(15, 23, 42, 0.9)"
                    stroke="#38bdf8"
                    strokeWidth="1"
                  />
                  <text x="8" y="16" fill="#94a3b8" fontSize="11px">
                    Cá thể/Thế hệ: <tspan fill="#ffffff" fontWeight="bold">#{hoverIndex}</tspan>
                  </text>
                  <text x="8" y="32" fill="#94a3b8" fontSize="11px">
                    Điểm phạt: <tspan fill="#f87171" fontWeight="bold">{duLieu[hoverIndex].toFixed(2)}</tspan>
                  </text>
                </g>
              </g>
            )}
          </svg>
        </div>

        {/* Khung tóm tắt thông số bên dưới đồ thị */}
        <div className="row g-2 mt-2 pt-2 border-top text-center" style={{ fontSize: '13px' }}>
          <div className="col-6 col-md-3">
            <span className="text-muted d-block small">Điểm phạt ban đầu (G0):</span>
            <strong className="text-danger">{diemDau.toFixed(2)}</strong>
          </div>
          <div className="col-6 col-md-3">
            <span className="text-muted d-block small">Điểm phạt tối ưu:</span>
            <strong className="text-success">{diemTotNhat.toFixed(2)}</strong>
          </div>
          <div className="col-6 col-md-3">
            <span className="text-muted d-block small">Số cá thể / thế hệ:</span>
            <strong className="text-primary">{tongSoDiem}</strong>
          </div>
          <div className="col-6 col-md-3">
            <span className="text-muted d-block small">Độ cải thiện chất lượng:</span>
            <strong className="text-info">{tyLeCaiThien}%</strong>
          </div>
        </div>
      </div>
    </div>
  )
}

export default DoThiTienHoaGA
