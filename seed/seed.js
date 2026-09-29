// Local development seed data, run once when the MongoDB volume is created (docker compose up -d).
// Building IDs match the outlines in the frontend src/constants/buildings.json;
// names come from OpenStreetMap, coordinates are the outline centroids, and addresses are OpenStreetMap street
// addresses where available, otherwise a place query Google Maps can resolve. Reviews and events are samples.
db = db.getSiblingDB("test");

db.building.insertMany([
  {"_id":ObjectId("673400991a2d8e0fbcc020f5"),"name":"Earhart Hall","acronym":"EAR","address":"1275 First Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.425843,"longitude":-86.925008},
  {"_id":ObjectId("673401181a2d8e0fbcc020f8"),"name":"Wiley Dining Court","acronym":"WDCT","address":"498 Jischke Drive, West Lafayette, IN 47906","buildingType":"DinningCourt","latitude":40.428529,"longitude":-86.920903},
  {"_id":ObjectId("67abef4b4d435cf0e658adad"),"name":"Shreve Hall","acronym":"SHRV","address":"1275 Third Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.426759,"longitude":-86.924995},
  {"_id":ObjectId("67a2bf3f41447d8654fec45e"),"name":"Hillenbrand Hall","acronym":"HILL","address":"1301 Third Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.426688,"longitude":-86.926665},
  {"_id":ObjectId("67ae9c55320274ba7a3cffb6"),"name":"Hawkins Hall","acronym":"HAWK","address":"430 West Wood Street, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.422798,"longitude":-86.91165},
  {"_id":ObjectId("67ae9f0c320274ba7a3cffb8"),"name":"First Street Towers","acronym":"FST","address":"1230 First Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.424915,"longitude":-86.924541},
  {"_id":ObjectId("673401591a2d8e0fbcc020f9"),"name":"Windsor Halls","acronym":"WIND","address":"Windsor Halls, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.426299,"longitude":-86.920847},
  {"_id":ObjectId("67b524dd7e2b227430be039c"),"name":"Meredith Hall","acronym":"MRDH","address":"201 Martin Jischke Drive, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.426353,"longitude":-86.923295},
  {"_id":ObjectId("67b526f07e2b227430be039d"),"name":"Meredith Hall South","acronym":"MRDS","address":"1225 First Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.425488,"longitude":-86.923218},
  {"_id":ObjectId("67b52af17e2b227430be039e"),"name":"Winifred Parker Hall","acronym":"PKRW","address":"1196 Third Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.427756,"longitude":-86.920265},
  {"_id":ObjectId("67b52bb47e2b227430be039f"),"name":"Frieda Parker Hall","acronym":"PKRF","address":"Frieda Parker Hall, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.42827,"longitude":-86.919508},
  {"_id":ObjectId("67a2cd6841447d8654fec460"),"name":"Ford Dining Court","acronym":"FORD","address":"1122 West Stadium Avenue, West Lafayette, IN 47906","buildingType":"DinningCourt","latitude":40.432087,"longitude":-86.919564},
  {"_id":ObjectId("67b52c0e7e2b227430be03a0"),"name":"University Residences","acronym":null,"address":"University Residences, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.434491,"longitude":-86.92078},
  {"_id":ObjectId("67ae9e4cb54f3608bced571c"),"name":"Honors College and Residences North","acronym":"HCRN","address":"Honors College and Residences North, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.426759,"longitude":-86.919665},
  {"_id":ObjectId("67b531027e2b227430be03a1"),"name":"Honors College and Residences South","acronym":"HCRS","address":"201 North Russell Street, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.425854,"longitude":-86.919471},
  {"_id":ObjectId("67b531fe7e2b227430be03a2"),"name":"Cary Quadrangle","acronym":"CARY","address":"1016 West Stadium Avenue, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.432124,"longitude":-86.917903},
  {"_id":ObjectId("67b534bccb4412c68729fd34"),"name":"Tarkington Hall","acronym":"TARK","address":"Tarkington Hall, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.430627,"longitude":-86.920732},
  {"_id":ObjectId("67b53552cb4412c68729fd35"),"name":"Wiley Hall","acronym":"WILY","address":"500 North Martin Jischke Drive, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.42948,"longitude":-86.920743},
  {"_id":ObjectId("67b7d208fc18b9daaaee6fbb"),"name":"Owen Hall","acronym":"OWEN","address":"Owen Hall, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.432288,"longitude":-86.920743},
  {"_id":ObjectId("67b7d278fc18b9daaaee6fbc"),"name":"Harrison Hall","acronym":"HARR","address":"Harrison Hall, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.425068,"longitude":-86.926822},
  {"_id":ObjectId("67b7d445fc18b9daaaee6fbd"),"name":"McCutcheon Hall","acronym":"MCUT","address":"McCutcheon Hall, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.425068,"longitude":-86.92804},
  {"_id":ObjectId("b3bfe5042cdbd04fbd025b0f"),"name":"South Hall","acronym":"SH","address":"201 North MacArthur Drive, West Lafayette, IN 47906","buildingType":"Housing","latitude":40.426366,"longitude":-86.925769}
]);

const daysFromNow = (days, hour) => {
  const date = new Date();
  date.setUTCDate(date.getUTCDate() + days);
  date.setUTCHours(hour, 0, 0, 0);
  return date;
};

db.review.insertMany([
  { userId: "sample-user", buildingId: "673400991a2d8e0fbcc020f5", rating: 8, description: "Sample review: close to Earhart Dining Court and quiet on weekdays.", createdAt: daysFromNow(-12, 15), likeCount: 3, dislikeCount: 0, flagged: false },
  { userId: "sample-user", buildingId: "673400991a2d8e0fbcc020f5", rating: 6, description: "Sample review: rooms are small but the lounges are great for studying.", createdAt: daysFromNow(-4, 18), likeCount: 1, dislikeCount: 1, flagged: false },
  { userId: "sample-user", buildingId: "67a2bf3f41447d8654fec45e", rating: 9, description: "Sample review: spacious suites and a short walk to the CoRec.", createdAt: daysFromNow(-7, 20), likeCount: 5, dislikeCount: 0, flagged: false },
  { userId: "sample-user", buildingId: "673401181a2d8e0fbcc020f8", rating: 7, description: "Sample review: good variety, busiest right after noon classes.", createdAt: daysFromNow(-2, 17), likeCount: 2, dislikeCount: 0, flagged: false },
]);

db.events.insertMany([
  { eventName: "Residence Hall Open House", summary: "Sample event: tour rooms and meet resident assistants.", content: "Sample event created by the local seed script.", userID: "sample-user", date: daysFromNow(3, 22), address: "1301 Third Street, West Lafayette, IN 47906" },
  { eventName: "Late Night Study Session", summary: "Sample event: snacks and quiet study space during finals week.", content: "Sample event created by the local seed script.", userID: "sample-user", date: daysFromNow(7, 1), address: "1275 First Street, West Lafayette, IN 47906" },
  { eventName: "Dining Court Tasting", summary: "Sample event: try new menu items and vote for favorites.", content: "Sample event created by the local seed script.", userID: "sample-user", date: daysFromNow(10, 17), address: "498 Jischke Drive, West Lafayette, IN 47906" },
]);
