import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 40000,
});

export const fetchData = async (message) => {
  try {
    const res = await apiClient.post("/api/v1/ask", { Message: message });
    console.log("Получили ответ:", res);
    return res.data;
  } catch (e) {
    console.error("Ошибка при запросе:", e);
    throw e;
  }
};
