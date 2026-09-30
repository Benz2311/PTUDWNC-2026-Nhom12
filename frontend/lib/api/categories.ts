import axios from "axios";
import type {
  Category,
  CategoryStatistics,
} from "@/types/category";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5000/api/v1";

const api = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export async function getCategories(): Promise<Category[]> {
  const response = await api.get<Category[]>(
    "/categories"
  );

  return response.data;
}

export async function getCategoryStatistics():
  Promise<CategoryStatistics> {
  const response =
    await api.get<CategoryStatistics>(
      "/categories/statistics"
    );

  return response.data;
}