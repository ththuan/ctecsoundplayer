const fs = require('node:fs');
const path = require('node:path');
const { DatabaseSync } = require('node:sqlite');

const token = '{ROOT}';
const sourceCandidates = [
  process.env.CTEC_SEED_DIR,
  path.join(process.env.LOCALAPPDATA || '', 'CTECSoundPlayer'),
  path.join(__dirname, '..', '..', 'CTECSoundPlayer')
].filter(Boolean);
const source = sourceCandidates.find((dir) => fs.existsSync(path.join(dir, 'kho_nhac.db')));
const outDir = path.join(__dirname, '..', 'payload');
const stage = path.join(outDir, '.stage');

const dbName = 'kho_nhac.db';
const copyFiles = ['kich_ban.json', 'tuy_chinh.json', 'nhac_mien_phi_nguon.json'];
const copyDirs = ['Nhac da chep', 'Hieu ung'];

if (!source) {
  console.error('Không thấy kho_nhac.db trong LocalAppData hay thư mục CTECSoundPlayer.');
  process.exit(1);
}

fs.rmSync(stage, { recursive: true, force: true });
fs.mkdirSync(stage, { recursive: true });

const sourceDb = path.join(source, dbName);
const checkpoint = new DatabaseSync(sourceDb);
checkpoint.exec('PRAGMA wal_checkpoint(TRUNCATE)');
checkpoint.close();

fs.copyFileSync(sourceDb, path.join(stage, dbName));

const db = new DatabaseSync(path.join(stage, dbName));
db.exec('PRAGMA journal_mode=DELETE');
const row = db.prepare('SELECT COUNT(*) AS c FROM tracks').get();
db.prepare('UPDATE tracks SET path=replace(path,?,?) WHERE instr(path,?)>0').run(source, token, source);
db.prepare('UPDATE tracks SET watch_root=replace(watch_root,?,?) WHERE watch_root IS NOT NULL AND instr(watch_root,?)>0').run(source, token, source);
db.prepare('UPDATE tracks SET path=replace(path,?,?) WHERE instr(path,?)>0').run('\\Nhac da chep\\T-Sound\\', '\\Nhac da chep\\Nhac su kien\\', '\\Nhac da chep\\T-Sound\\');
db.prepare("UPDATE kv SET value=replace(value,'TS · ','CTEC · ') WHERE key='nhom'").run();
db.prepare("DELETE FROM kv WHERE key='phien'").run();
db.exec('VACUUM');
db.close();
for (const suffix of ['-wal', '-shm', '-journal']) {
  fs.rmSync(path.join(stage, dbName + suffix), { force: true });
}

for (const name of copyFiles) {
  const from = path.join(source, name);
  if (!fs.existsSync(from)) continue;
  let text = fs.readFileSync(from, 'utf8').replace(/^\uFEFF/, '');
  if (name === 'tuy_chinh.json') {
    try {
      const settings = JSON.parse(text);
      settings.DeviceName = null;
      settings.ZaloDir = null;
      settings.WatchFolders = [];
      text = JSON.stringify(settings, null, 2);
    } catch {
      // giữ nguyên nếu file không hợp lệ
    }
  } else {
    text = text.split(source).join(token);
  }
  fs.writeFileSync(path.join(stage, name), text, 'utf8');
}

for (const name of copyDirs) {
  const from = path.join(source, name);
  if (fs.existsSync(from)) fs.cpSync(from, path.join(stage, name), { recursive: true });
}

const oldEventDir = path.join(stage, 'Nhac da chep', 'T-Sound');
const newEventDir = path.join(stage, 'Nhac da chep', 'Nhac su kien');
if (fs.existsSync(oldEventDir)) fs.renameSync(oldEventDir, newEventDir);

const catalogSource = [path.join(source, 'tsound-catalog.json'), path.join(source, 'danh_muc_mau.json')].find((file) => fs.existsSync(file));
if (catalogSource) {
  const catalog = JSON.parse(fs.readFileSync(catalogSource, 'utf8').replace(/^\uFEFF/, ''));
  for (const item of catalog) {
    for (const key of ['url', 'source', 'sha256', 'bytes', 'format', 'processing']) delete item[key];
    for (const key of ['title', 'artist', 'license']) {
      if (typeof item[key] === 'string') item[key] = item[key].replace(/T-?Sound/gi, '').replace(/\s+/g, ' ').trim();
    }
  }
  fs.writeFileSync(path.join(stage, 'danh_muc_mau.json'), JSON.stringify(catalog, null, 2), 'utf8');
}

let files = 0;
let bytes = 0;
function walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full);
    else {
      files++;
      bytes += fs.statSync(full).size;
    }
  }
}
walk(stage);

fs.writeFileSync(
  path.join(outDir, 'manifest.json'),
  JSON.stringify(
    {
      createdAt: new Date().toISOString(),
      source,
      tracks: row.c,
      files,
      bytes,
      token
    },
    null,
    2
  ),
  'utf8'
);

console.log(`Đã chuẩn bị ${files} file (${(bytes / 1048576).toFixed(1)} MB), ${row.c} bài hát.`);
