# Use official Node.js runtime as base image
FROM node:18-alpine

# Set working directory in container
WORKDIR /app

# Copy package.json and package-lock.json
COPY Backend/package*.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application code
COPY Backend/ ./

# Expose port 3000 (default for this server, overridable via PORT env var)
EXPOSE 3000

# Set environment variables (these will be overridden by Render)
ENV NODE_ENV=production

# Start the server
CMD ["node", "server.js"]
