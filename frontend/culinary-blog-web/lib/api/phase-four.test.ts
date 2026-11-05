import { ApiError, apiClient } from "./client";
import { phaseFourApi } from "./phase-four";

describe("phaseFourApi", () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  it("reads a follow status from the API envelope", async () => {
    jest.spyOn(apiClient, "get").mockResolvedValueOnce({
      data: { data: { following: true } },
    } as never);

    await expect(phaseFourApi.getFollowStatus("author-id")).resolves.toBe(true);
    expect(apiClient.get).toHaveBeenCalledWith("/users/author-id/follow-status");
  });

  it("treats a missing nutrition goal as not configured", async () => {
    jest.spyOn(apiClient, "get").mockRejectedValueOnce(new ApiError("Not found", { status: 404 }));

    await expect(phaseFourApi.getNutritionGoals()).resolves.toBeNull();
  });

  it("sends ingredient suggestions to the server API", async () => {
    jest.spyOn(apiClient, "post").mockResolvedValueOnce({
      data: { recipes: [{ id: "recipe-id", title: "Soup", slug: "soup", imageUrl: null, ingredients: ["tomato"] }] },
    } as never);

    await expect(phaseFourApi.suggestRecipes(["tomato"])).resolves.toEqual([
      { id: "recipe-id", title: "Soup", slug: "soup", imageUrl: null, ingredients: ["tomato"] },
    ]);
    expect(apiClient.post).toHaveBeenCalledWith("/ai/suggest-by-ingredients", { ingredients: ["tomato"] });
  });
});
