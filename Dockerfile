# Use official Node.js runtime as base image
FROM node:22-alpine

# Set working directory in container
WORKDIR /app

# Copy package.json and package-lock.json
COPY Candycraze/Server/package*.json ./

# Install dependencies
RUN npm ci --omit=dev

# Copy the rest of the application code
COPY Candycraze/Server/app.js Candycraze/Server/google.js Candycraze/Server/profile.js Candycraze/Server/billing.js Candycraze/Server/server.js ./
USER node

# Expose port 3000 (default for this server, overridable via PORT env var)
EXPOSE 3000

# Set environment variables (these will be overridden by Render)
ENV NODE_ENV=production

# Start the server
CMD ["node", "server.js"]
