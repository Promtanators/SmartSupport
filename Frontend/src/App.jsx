import { useState } from "react";
import UserPage from "./pages/UserPage";
import { SupportPage } from "./pages";

function App() {
  const [currentPage, setCurrentPage] = useState(0);
   const [message, setMessage] = useState(""); 

  const handleClick = () => {
    console.log("Кнопка нажата");
    setCurrentPage(1);
  };

  return (
    <>
      {currentPage === 0 && (
        <UserPage
          handleClick={handleClick}
          message={message}
          setMessage={setMessage}
        />
      )}
      {currentPage === 1 && <SupportPage message={message} />}
    </>
  );
}

export default App;
