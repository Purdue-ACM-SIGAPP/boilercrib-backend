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
  {"_id":ObjectId("67b7d445fc18b9daaaee6fbd"),"name":"McCutcheon Hall","acronym":"MCUT","address":"McCutcheon Hall, Purdue University, West Lafayette, IN 47907","buildingType":"Housing","latitude":40.425068,"longitude":-86.92804}
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

// Sample students for the roommate finder. Log in as any of them with the password "password"
// (passwordHashed is that password hashed by PasswordHasherService).
const samplePasswordHash = "AQAAAAIAAYagAAAAEJa4HP4mmH0fM3LHXDw6xZJqRCrI2QFM7CITG6cECpsLEahbOse2+K8Nv5rmkvBr8A==";
db.user.insertMany([
  { _id: ObjectId("6740a0000000000000000001"), username: "jordan", passwordHashed: samplePasswordHash, name: "Jordan Lee", phoneNumber: "765-555-0101", accountType: 0 },
  { _id: ObjectId("6740a0000000000000000002"), username: "sam", passwordHashed: samplePasswordHash, name: "Sam Patel", phoneNumber: "765-555-0102", accountType: 0 },
  { _id: ObjectId("6740a0000000000000000003"), username: "riley", passwordHashed: samplePasswordHash, name: "Riley Chen", phoneNumber: "765-555-0103", accountType: 0 },
  { _id: ObjectId("6740a0000000000000000004"), username: "alex", passwordHashed: samplePasswordHash, name: "Alex Morgan", phoneNumber: "765-555-0104", accountType: 0 },
  { _id: ObjectId("6740a0000000000000000005"), username: "taylor", passwordHashed: samplePasswordHash, name: "Taylor Brooks", phoneNumber: "765-555-0105", accountType: 0 },
]);

// year is a ClassYear (0 = Freshman). Lifestyle answers are 1-5: sleepSchedule 1 = early bird,
// cleanliness 5 = very tidy, noiseTolerance 1 = needs quiet, guestFrequency 1 = rarely has guests.
// Budgets are dollars per semester. contactInfo is only shown after a roommate request is accepted.
db.roommateBio.insertMany([
  { userId: "6740a0000000000000000001", name: "Jordan Lee", gender: "Male", age: 18, year: 0, major: "Computer Science", bio: "Sample bio: up early for the gym and keeps the room tidy.", interests: ["gym", "coding", "basketball"], preferredBuildingIds: ["67a2bf3f41447d8654fec45e", "67ae9e4cb54f3608bced571c"], contactInfo: "jordan@example.com", createdAt: daysFromNow(-10, 15), term: "Fall 2027", budgetMin: 4000, budgetMax: 5500, sleepSchedule: 1, cleanliness: 5, noiseTolerance: 2, guestFrequency: 2, smoker: false, isLooking: true },
  { userId: "6740a0000000000000000002", name: "Sam Patel", gender: "Male", age: 18, year: 0, major: "Mechanical Engineering", bio: "Sample bio: quiet weeknights, game nights on weekends.", interests: ["gym", "video games"], preferredBuildingIds: ["67a2bf3f41447d8654fec45e", "673400991a2d8e0fbcc020f5"], contactInfo: "sam@example.com", createdAt: daysFromNow(-8, 18), term: "Fall 2027", budgetMin: 4500, budgetMax: 6000, sleepSchedule: 2, cleanliness: 4, noiseTolerance: 2, guestFrequency: 3, smoker: false, isLooking: true },
  { userId: "6740a0000000000000000003", name: "Riley Chen", gender: "Female", age: 19, year: 1, major: "Psychology", bio: "Sample bio: night owl who studies late and likes having friends over.", interests: ["music", "theater"], preferredBuildingIds: ["67b531fe7e2b227430be03a2", "67b534bccb4412c68729fd34"], contactInfo: "riley@example.com", createdAt: daysFromNow(-6, 21), term: "Fall 2027", budgetMin: 3000, budgetMax: 4200, sleepSchedule: 5, cleanliness: 2, noiseTolerance: 5, guestFrequency: 5, smoker: false, isLooking: true },
  { userId: "6740a0000000000000000004", name: "Alex Morgan", gender: "Nonbinary", age: 18, year: 0, major: "Biology", bio: "Sample bio: honors student, flexible schedule, pretty easygoing.", interests: ["coding", "hiking"], preferredBuildingIds: ["67ae9e4cb54f3608bced571c", "67b531027e2b227430be03a1"], contactInfo: "alex@example.com", createdAt: daysFromNow(-3, 16), term: "Fall 2027", budgetMin: 5000, budgetMax: 6500, sleepSchedule: 3, cleanliness: 4, noiseTolerance: 3, guestFrequency: 2, smoker: false, isLooking: true },
  { userId: "6740a0000000000000000005", name: "Taylor Brooks", gender: "Female", age: 20, year: 2, major: "Management", bio: "Sample bio: looking for a roommate for spring.", interests: ["running"], preferredBuildingIds: ["673401591a2d8e0fbcc020f9"], contactInfo: "taylor@example.com", createdAt: daysFromNow(-1, 14), term: "Spring 2027", budgetMin: 3500, budgetMax: 5000, sleepSchedule: 3, cleanliness: 3, noiseTolerance: 3, guestFrequency: 3, smoker: false, isLooking: true },
]);
