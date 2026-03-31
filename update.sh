#!/bin/bash
set -e

echo "Pulling latest images..."
docker compose pull backend
docker compose pull frontend

echo "Restarting containers..."
docker compose up -d

echo ""
echo "Running containers:"
docker ps

echo ""
echo "Caddy status:"
systemctl status caddy --no-pager
