# dev hosts the separate Monster Merge test service.
FROM node:22-alpine AS dependencies
WORKDIR /app
COPY MonsterMergeServer/package.json MonsterMergeServer/package-lock.json ./
RUN npm ci --omit=dev --ignore-scripts && npm cache clean --force
FROM node:22-alpine
ENV NODE_ENV=production PORT=3000
ENV PUBLIC_URL=https://candycraze.onrender.com
ENV MONSTER_MONGODB_DATABASE=monstermerge
WORKDIR /app
COPY --from=dependencies --chown=node:node /app/node_modules ./node_modules
COPY --chown=node:node MonsterMergeServer/ ./
USER node
EXPOSE 3000
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 CMD wget -qO- "http://127.0.0.1:${PORT}/health" >/dev/null || exit 1
CMD ["node", "server.js"]
