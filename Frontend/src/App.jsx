import { useState } from "react";
import UserPage from "./pages/UserPage";
import { SupportPage } from "./pages";

function App() {
  const [currentPage, setCurrentPage] = useState(0);
  const [message, setMessage] = useState("");

  return (
    <>
      {currentPage === 0 && (
        <UserPage
          onNextPage={() => setCurrentPage(1)}
          message={message}
          setMessage={setMessage}
        />
      )}
      {currentPage === 1 && <SupportPage message={message} />}
    </>
  );
}

export default App;
