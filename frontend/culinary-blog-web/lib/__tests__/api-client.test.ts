import { apiClient, API_BASE_URL } from "../api/client";
import { getCategories } from "../api/categories";

describe("Frontend API Client & Backend Connection Config", () => {
  it("should configure API_BASE_URL from environment or fallback", () => {
    expect(API_BASE_URL).toBeDefined();
    expect(typeof API_BASE_URL).toBe("string");
    expect(API_BASE_URL).toMatch(/http:\/\/localhost:5000\/api\/v1/);
  });

  it("apiClient should be instantiated with correct headers and baseURL", () => {
    expect(apiClient.defaults.baseURL).toBe(API_BASE_URL);
    expect(apiClient.defaults.headers["Content-Type"]).toBe("application/json");
  });

  it("getCategories should call backend categories endpoint and handle responses", async () => {
    const mockCategories = [
      { id: "1", name: "Món Chay", slug: "mon-chay", description: "Các món chay thanh đạm", recipeCount: 5 },
    ];

    jest.spyOn(apiClient, "get").mockResolvedValueOnce({ data: mockCategories });

    const result = await getCategories();
    expect(apiClient.get).toHaveBeenCalledWith("/categories");
    expect(result).toEqual(mockCategories);
  });
});
