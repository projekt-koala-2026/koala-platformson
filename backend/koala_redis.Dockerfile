FROM redis:8-alpine

EXPOSE 6379

# Execute redis with inline configuration populated by runtime environment variables
CMD ["sh", "-c", "exec redis-server \
    --requirepass \"$REDIS_PASSWORD\" \
    --maxmemory \"$REDIS_MAXMEMORY\" \
    --maxmemory-policy \"$REDIS_MAXMEMORY_POLICY\""]