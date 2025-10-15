/*Пример, как делать апи-запросы. Использовать Axios, 
вынести путь в отдельную переменную

import axios, { Axios } from "axios";
import { saveAs } from "file-saver";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 7000,
});

export const uploadFiles = async (formData) => {
  try {
    const res = await apiClient.post("/upload", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    return res;
  } catch (e) {
    console.error("Ошибка загрузки файла:", e);
    throw e;
  }
};

*/
