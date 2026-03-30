#!/bin/bash
REGISTRY=thewayuch

echo "Building frontend..."
docker build -t $REGISTRY/frontend:latest -f Dockerfile.frontend .

echo "Building backend..."
docker build -t $REGISTRY/backend:latest -f Dockerfile.backend .

echo "Pushing images..."
docker push $REGISTRY/frontend:latest
docker push $REGISTRY/backend:latest

echo ""
echo "Done! Now run on the server:"
echo "  docker compose pull && docker compose up -d"