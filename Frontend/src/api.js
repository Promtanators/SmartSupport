import axios from "axios";

const apiClient = axios.create({
  baseURL: "http://localhost:5000", // базовый URL бекенда
  timeout: 7000,
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
