#!/bin/bash

set -e

# Configuration
PROJECT_ID="threadsmcpnet"
REGION="europe-west1"
REPOSITORY="threads-mcp-net"
IMAGE_NAME="threads-mcp-net-1"
TAG="latest"
PLATFORM="linux/amd64"

# Construct full image path
DOCKER_IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPOSITORY}/${IMAGE_NAME}:${TAG}"

# Build and push for linux/amd64 (Cloud Run platform)
echo "🏗️  Building for ${PLATFORM}..."
docker build --platform ${PLATFORM} -t ${IMAGE_NAME} .
docker tag ${IMAGE_NAME} ${DOCKER_IMAGE}
docker push ${DOCKER_IMAGE}
echo ""
echo "✅ Build and push complete!"
echo "🐳 Image: ${DOCKER_IMAGE}"

