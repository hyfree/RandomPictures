docker buildx create --name lilqbuilder
docker buildx use lilqbuilder
docker buildx ls
docker buildx inspect lilqbuilder --bootstrap
docker buildx ls
docker buildx build --platform linux/amd64,linux/arm64  -t registry.cn-hangzhou.aliyuncs.com/hyfree/random_image:0.0.3  --push .