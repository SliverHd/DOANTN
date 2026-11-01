import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import Header from '../components/Header'
import Sidebar from '../components/Sidebar'

function MainLayout() {
  const [sidebarThuGon, setSidebarThuGon] = useState(false)

  return (
    <div className="app-shell">
      <Sidebar thuGon={sidebarThuGon} />
      <div className="main-panel">
        <Header
          sidebarThuGon={sidebarThuGon}
          onToggleSidebar={() => setSidebarThuGon((prev) => !prev)}
        />
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default MainLayout
