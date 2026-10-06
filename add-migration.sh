#!/bin/bash
set -e

export PATH="$PATH:/root/.dotnet/tools"

cd /app
dotnet restore
dotnet build --no-restore

dotnet tool install --global dotnet-ef --version 10.0.12 2>/dev/null || true

cd /app/src/CulinaryBlog.Infrastructure
dotnet ef migrations add OptimizeAuthIndexes --project . --startup-project ../CulinaryBlog.Api