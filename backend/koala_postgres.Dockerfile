FROM postgres:16-alpine

EXPOSE 5432

# Persistent database storage mount point
VOLUME /var/lib/postgresql/data