const {createGooglePlayApi}=require('../../utils/google-play');
const {configuration,coinProducts}=require('./config');
module.exports={...createGooglePlayApi(configuration()),coinProducts};
