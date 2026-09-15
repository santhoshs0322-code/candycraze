FROM node:22-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --omit=dev
COPY app.js google.js profile.js billing.js server.js ./
ENV NODE_ENV=production
USER node
EXPOSE 3000
CMD ["node", "server.js"]
