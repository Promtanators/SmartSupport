import { useState } from "react";
import UserPage from "./pages/UserPage";
import { SupportPage } from "./pages";

function App() {
  const [currentPage, setCurrentPage] = useState(0);
  const [message, setMessage] = useState("");
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);

  return (
    <>
      {currentPage === 0 && (
        <UserPage
          onNextPage={() => setCurrentPage(1)}
          message={message}
          setMessage={setMessage}
          setData={setData}
          setError={setError}
        />
      )}
      {currentPage === 1 && (
        <SupportPage
          message={message}
          data={data}
          error={error}
          setData={setData}
          setError={setError}
        />
      )}
    </>
  );
}

export default App;
