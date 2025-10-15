import { useState } from "react";
import UserPage from "../pages/UserPage";
import { SupportPage } from "../pages";
// npm install react-tsparticles tsparticles-slim библиотеки для анимации для первой страницы
function App() {
  const [currentPage, setCurrentPage] = useState(0);

  const handleClick = () => {
    console.log("Кнопка нажата");
    setCurrentPage(1);
  };

  return (
    <>
      {currentPage === 0 && <UserPage handleClick={handleClick} />}
      {currentPage === 1 && <SupportPage />}
    </>
  );
}

export default App;
