# ARRoomTransformer 🏠➡️👾

**Gerçek odanızı AR ile dönüştürün — Backrooms deneyimi yaşayın!**

## 🎯 Proje Hakkında

ARRoomTransformer, iPhone'unuzun kamerası ve sensörlerini kullanarak gerçek bir odayı tarayıp, 3D asset'lerle tamamen farklı bir mekana dönüştürmenizi sağlayan bir AR (Artırılmış Gerçeklik) uygulamasıdır.

### Temel Özellikler
- 📐 **Oda Tarama** — ARKit ile odanızın duvarlarını, zeminini ve tavanını algılayın
- 📍 **Köşe İşaretleme** — Odanın köşelerini dokunarak işaretleyin
- 🏗️ **Duvar/Zemin Oluşturma** — İşaretlenen köşelerden otomatik duvar ve zemin mesh'leri
- 🪑 **Asset Yerleştirme** — Katalogdan 3D objeleri seçip odaya yerleştirin
- 🔄 **Transform Kontrolleri** — Taşıma, döndürme, boyutlandırma (pinch & drag)
- 💡 **Gerçekçi Işıklandırma** — Ortam ışığını algılayıp sanal objelere uygulama
- 👤 **Occlusion** — Gerçek objelerin sanal objeleri kapatması (LiDAR destekli)
- 🎥 **AR Video Kayıt** — Dönüştürülmüş odanızın videosunu çekin
- 💾 **Sahne Kayıt/Yükleme** — Projelerinizi kaydedin ve tekrar açın
- 🎨 **Tema Desteği** — Backrooms ve diğer tematik materyal setleri

## 🛠️ Gereksinimler

### Geliştirme Ortamı
- **Unity 2022.3 LTS** veya üzeri
- **Xcode 15** veya üzeri (iOS build için)
- **macOS** (iOS build için gerekli)

### Hedef Cihaz
- **iPhone** — iOS 16.0 veya üzeri
- **Önerilen**: iPhone 12 Pro+ (LiDAR sensörlü modeller en iyi deneyimi sunar)
- iPhone SE 2. nesil ve üzeri tüm modellerde çalışır (LiDAR olmadan sınırlı occlusion)

### Unity Paketleri (otomatik yüklenir)
- AR Foundation 5.1.5
- ARKit XR Plugin 5.1.5
- ARCore XR Plugin 5.1.5
- Universal Render Pipeline (URP) 14.x
- TextMeshPro 3.x
- Newtonsoft JSON
- Input System 1.7.x

## 🚀 Kurulum

### 1. Unity Hub'dan Projeyi Açın
1. Unity Hub'ı açın
2. **"Open" → "Add project from disk"** seçin
3. `ARRoomTransformer` klasörünü seçin
4. Unity 2022.3 LTS ile açın

### 2. Platform Ayarları
1. **File → Build Settings** açın
2. **iOS** platformunu seçin
3. **"Switch Platform"** tıklayın

### 3. Proje Ayarları
1. **Edit → Project Settings → XR Plug-in Management**
2. iOS sekmesinde **"Apple ARKit"** işaretleyin
3. **Player Settings → Other Settings**:
   - Camera Usage Description: `"AR deneyimi için kamera erişimi gereklidir"`
   - Location Usage Description: `"Konum tabanlı AR deneyimi için"`
   - Minimum iOS Version: `16.0`
   - Architecture: `ARM64`

### 4. URP Yapılandırması
1. **Edit → Project Settings → Graphics**
2. URP Asset atayın (yoksa oluşturun: Create → Rendering → URP Asset)

### 5. Sahne Kurulumu
1. `Assets/Scenes` altında yeni bir sahne oluşturun
2. Sahneye şu objeleri ekleyin:
   - **AR Session** (Add Component: AR Session)
   - **XR Origin** (Add Component: XR Origin, AR Camera Manager, AR Plane Manager, AR Raycast Manager, AR Anchor Manager)
   - **Directional Light** (ARLightEstimation script'i ekleyin)
   - **Canvas** (UI panelleri için)
   - **AppManager** (boş GameObject'e ekleyin)

## 📁 Proje Yapısı

```
ARRoomTransformer/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/               # AR oturum ve uygulama yönetimi
│   │   ├── RoomMapping/        # Oda tarama ve mesh oluşturma
│   │   ├── AssetPlacement/     # 3D obje yerleştirme
│   │   ├── Rendering/          # Işık, occlusion, materyal
│   │   ├── Recording/          # Video kayıt
│   │   ├── UI/                 # Kullanıcı arayüzü
│   │   └── Data/               # Veri modelleri, kayıt/yükleme
│   ├── Prefabs/                # Hazır prefab'lar
│   ├── Materials/              # Materyal dosyaları
│   ├── Textures/               # Texture dosyaları
│   └── Scenes/                 # Unity sahneleri
├── Packages/
│   └── manifest.json           # Paket bağımlılıkları
└── README.md
```

## 🎮 Kullanım Akışı

```
1. Uygulamayı Aç → Ana Menü
2. "Yeni Tarama" → Oda Tarama Modu
   ├── Telefonu yavaşça çevir (yüzey algılama)
   ├── Odanın köşelerini işaretle (min. 4 köşe)
   └── "Onayla" → Duvarlar/zemin oluşturulur
3. Asset Yerleştirme Modu
   ├── Katalogdan asset seç
   ├── Odaya dokunarak yerleştir
   ├── Taşı / Döndür / Boyutlandır
   └── Tema uygula (Backrooms vb.)
4. Kayıt Modu
   ├── Kayıt başlat (3-2-1 geri sayım)
   ├── Odada dolaşarak video çek
   └── Kayıt durdur → Galeriye kaydet
```

## 🎨 Backrooms Teması

Varsayılan tema olarak Backrooms stili dahildir:
- Sarımsı-kirli duvar kağıdı
- Nemli görünümlü halı zemin
- Floresan tavan aydınlatması
- Karanlık köşe efektleri

> **Not**: Kendi texture'larınızı `Assets/Textures/Themes/` altına ekleyerek özel temalar oluşturabilirsiniz.

## 📝 Sonraki Adımlar (Roadmap)

- [ ] Sesli ortam efektleri (ambiance)
- [ ] Multiplayer AR deneyimi
- [ ] Hazır sahne şablonları
- [ ] Asset Store entegrasyonu
- [ ] Android desteği (ARCore)
- [ ] Cloud anchor ile sahne paylaşımı
- [ ] Fizik simülasyonu (düşen objeler vb.)

## 📄 Lisans

Bu proje özel kullanım içindir. Ticari kullanım için izin gereklidir.

---

**Geliştirici**: Şamil  
**Versiyon**: 0.1.0 (Geliştirme Aşaması)
