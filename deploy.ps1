$REGISTRY = "thewayuch"

Write-Host "Building frontend..."
docker build -t "$REGISTRY/frontend:latest" -f Dockerfile.frontend .

Write-Host "Building backend..."
docker build -t "$REGISTRY/backend:latest" -f Dockerfile.backend .

Write-Host "Pushing images..."
docker push "$REGISTRY/frontend:latest"
docker push "$REGISTRY/backend:latest"

Write-Host ""
Write-Host "Done! Now run on the server:"
Write-Host "  docker compose pull && docker compose up -d"
