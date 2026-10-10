export default function StatisticsLoading() {
  return (
    <main className="min-h-screen bg-gray-50/60 py-10 px-4 sm:px-6 lg:px-8 animate-pulse">
      <div className="mx-auto max-w-6xl space-y-8">
        {/* Header Skeleton */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 sm:p-8 rounded-3xl border border-gray-100 shadow-xs">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <div className="h-2 w-2 rounded-full bg-gray-300" />
              <div className="h-3 w-44 rounded-md bg-gray-200" />
            </div>
            <div className="h-7 w-72 rounded-lg bg-gray-200" />
            <div className="h-4 w-96 rounded-md bg-gray-100" />
          </div>

          <div className="flex items-center gap-2">
            <div className="h-9 w-32 rounded-xl bg-gray-200" />
            <div className="h-9 w-32 rounded-xl bg-gray-200" />
          </div>
        </div>

        {/* Summary Cards Skeleton (5 cards) */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
          {[1, 2, 3, 4, 5].map((i) => (
            <div
              key={i}
              className={`rounded-3xl bg-white p-5 border border-gray-100 shadow-xs flex flex-col justify-between h-36 ${
                i === 5 ? "col-span-2 sm:col-span-1" : ""
              }`}
            >
              <div className="flex items-center justify-between">
                <div className="h-3 w-20 rounded bg-gray-200" />
                <div className="h-9 w-9 rounded-xl bg-gray-100" />
              </div>
              <div className="space-y-2 mt-4">
                <div className="h-8 w-14 rounded-lg bg-gray-200" />
                <div className="h-3 w-24 rounded bg-gray-100" />
              </div>
            </div>
          ))}
        </div>

        {/* Charts Row Skeleton */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Monthly Chart Skeleton */}
          <div className="lg:col-span-2 rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs space-y-6">
            <div className="flex items-center justify-between">
              <div className="space-y-2">
                <div className="h-5 w-48 rounded bg-gray-200" />
                <div className="h-3 w-64 rounded bg-gray-100" />
              </div>
              <div className="h-6 w-24 rounded-full bg-gray-100" />
            </div>

            <div className="h-60 w-full flex items-end gap-3 pt-6 border-b border-gray-100 pb-2">
              {[40, 70, 55, 85, 60, 95].map((h, idx) => (
                <div key={idx} className="flex-1 flex flex-col items-center justify-end h-full gap-2">
                  <div
                    style={{ height: `${h}%` }}
                    className="w-full max-w-[42px] rounded-t-xl bg-gray-200"
                  />
                  <div className="h-3 w-8 rounded bg-gray-100" />
                </div>
              ))}
            </div>
          </div>

          {/* Top Categories Skeleton */}
          <div className="rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs space-y-6">
            <div className="flex items-center justify-between">
              <div className="space-y-2">
                <div className="h-5 w-32 rounded bg-gray-200" />
                <div className="h-3 w-28 rounded bg-gray-100" />
              </div>
              <div className="h-6 w-6 rounded-full bg-gray-100" />
            </div>

            <div className="space-y-4">
              {[1, 2, 3, 4, 5].map((i) => (
                <div key={i} className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="h-3 w-28 rounded bg-gray-200" />
                    <div className="h-3 w-16 rounded bg-gray-100" />
                  </div>
                  <div className="h-2 w-full rounded-full bg-gray-100" />
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Table Skeleton */}
        <div className="rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs space-y-4">
          <div className="space-y-2 pb-3 border-b border-gray-100">
            <div className="h-5 w-60 rounded bg-gray-200" />
            <div className="h-3 w-72 rounded bg-gray-100" />
          </div>

          <div className="space-y-3 pt-2">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="flex items-center justify-between py-3 border-b border-gray-50">
                <div className="h-4 w-8 rounded bg-gray-100" />
                <div className="h-4 w-40 rounded bg-gray-200" />
                <div className="h-4 w-16 rounded bg-gray-100" />
                <div className="h-4 w-44 rounded bg-gray-200" />
              </div>
            ))}
          </div>
        </div>
      </div>
    </main>
  );
}
