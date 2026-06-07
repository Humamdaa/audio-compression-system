const fs = require('fs');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  AlignmentType, LevelFormat, HeadingLevel, BorderStyle, WidthType,
  ShadingType, TableOfContents, PageBreak, ImageRun
} = require('docx');

// Embed a PNG centered, scaled to wPx wide (keeps aspect from the IHDR header).
function imagePara(file, wPx) {
  const data = fs.readFileSync(file);
  const w = data.readUInt32BE(16), h = data.readUInt32BE(20);
  const hPx = Math.round(wPx * h / w);
  return new Paragraph({
    alignment: AlignmentType.CENTER, spacing: { before: 80, after: 160 },
    children: [new ImageRun({
      type: "png", data,
      transformation: { width: wPx, height: hPx },
      altText: { title: "screenshot", description: "Audio Compressor screenshot", name: "shot" }
    })]
  });
}
function caption(text) {
  return new Paragraph({
    bidirectional: true, alignment: AlignmentType.CENTER, spacing: { after: 200 },
    children: [new TextRun({ text, rightToLeft: true, font: "Arial", italics: true, size: 18, color: "5A5D8C" })]
  });
}

const FONT = "Arial";
const CONTENT_W = 9026; // A4 (11906) - 2*1440 margins

// ---------- run helpers ----------
const ar = (text, opts = {}) => new TextRun({ text, rightToLeft: true, font: FONT, ...opts });
const en = (text, opts = {}) => new TextRun({ text, rightToLeft: false, font: FONT, ...opts });

// ---------- paragraph helpers ----------
function P(children, opts = {}) {
  if (typeof children === 'string') children = [ar(children)];
  return new Paragraph({
    bidirectional: true, alignment: AlignmentType.RIGHT,
    spacing: { after: 120, line: 288 }, children, ...opts
  });
}
function center(children, opts = {}) {
  if (typeof children === 'string') children = [ar(children)];
  return new Paragraph({
    bidirectional: true, alignment: AlignmentType.CENTER,
    spacing: { after: 140 }, children, ...opts
  });
}
function H1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1, bidirectional: true, alignment: AlignmentType.RIGHT,
    spacing: { before: 260, after: 140 }, children: [ar(text, { bold: true })]
  });
}
function H2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2, bidirectional: true, alignment: AlignmentType.RIGHT,
    spacing: { before: 180, after: 100 }, children: [ar(text, { bold: true })]
  });
}
function bullet(children) {
  if (typeof children === 'string') children = [ar(children)];
  return new Paragraph({
    bidirectional: true, alignment: AlignmentType.RIGHT,
    numbering: { reference: "bullets", level: 0 }, spacing: { after: 60 }, children
  });
}
function numItem(children) {
  if (typeof children === 'string') children = [ar(children)];
  return new Paragraph({
    bidirectional: true, alignment: AlignmentType.RIGHT,
    numbering: { reference: "numbers", level: 0 }, spacing: { after: 60 }, children
  });
}
function spacer() { return new Paragraph({ children: [ar("")], spacing: { after: 120 } }); }

// ---------- table helpers ----------
const bd = { style: BorderStyle.SINGLE, size: 1, color: "B0B7C3" };
const allBorders = { top: bd, bottom: bd, left: bd, right: bd };
function cell(content, w, { header = false, bg } = {}) {
  let runs;
  if (typeof content === 'string') runs = [ar(content, { bold: header })];
  else runs = content;
  return new TableCell({
    width: { size: w, type: WidthType.DXA },
    borders: allBorders,
    shading: bg ? { fill: bg, type: ShadingType.CLEAR } : undefined,
    margins: { top: 70, bottom: 70, left: 110, right: 110 },
    children: [new Paragraph({ bidirectional: true, alignment: AlignmentType.RIGHT, children: runs })]
  });
}
function table(widths, rows) {
  return new Table({
    width: { size: widths.reduce((a, b) => a + b, 0), type: WidthType.DXA },
    columnWidths: widths,
    visuallyRightToLeft: true,
    rows
  });
}
const HEADBG = "1F3864";
const headCell = (t, w) => cell([ar(t, { bold: true, color: "FFFFFF" })], w, { bg: HEADBG });

// =====================================================
// CONTENT
// =====================================================
const children = [];

// ---------- COVER ----------
children.push(new Paragraph({ children: [ar("")], spacing: { after: 600 } }));
children.push(center([ar("كلية الهندسة المعلوماتية", { bold: true, size: 28 })]));
children.push(center([ar("مشروع مقرّر الوسائط المتعددة 2026", { size: 26 })]));
children.push(new Paragraph({ children: [ar("")], spacing: { after: 500 } }));
children.push(center([ar("ضغط ملفات الصوت", { bold: true, size: 56, color: "1F3864" })]));
children.push(center([ar("تطبيق سطح مكتب ", { size: 30 }), en("(Desktop Application)", { size: 30 })]));
children.push(new Paragraph({ children: [ar("")], spacing: { after: 700 } }));
children.push(center([ar("تقرير المشروع العملي", { bold: true, size: 32 })]));
children.push(new Paragraph({ children: [ar("")], spacing: { after: 500 } }));
children.push(P([ar("إعداد الطلاب:", { bold: true })]));
children.push(P([ar("الاسم: ", { bold: true }), ar("..................................................")]));
children.push(P([ar("الاسم: ", { bold: true }), ar("..................................................")]));
children.push(P([ar("الاسم: ", { bold: true }), ar("..................................................")]));
children.push(P([ar("الاسم: ", { bold: true }), ar("..................................................")]));
children.push(P([ar("الاسم: ", { bold: true }), ar("..................................................")]));
children.push(spacer());
children.push(P([ar("مدرّس العملي: ", { bold: true }), ar("..................................................")]));
children.push(P([ar("القسم: ", { bold: true }), ar("..................................................")]));
children.push(P([ar("تاريخ التسليم: ", { bold: true }), ar("حزيران / يونيو 2026")]));
children.push(new Paragraph({ children: [new PageBreak()] }));

// ---------- TOC ----------
children.push(center([ar("جدول المحتويات", { bold: true, size: 30 })]));
children.push(new TableOfContents("جدول المحتويات", { hyperlink: true, headingStyleRange: "1-2" }));
children.push(new Paragraph({ children: [new PageBreak()] }));

// ---------- 1. مقدمة ----------
children.push(H1("1. المقدمة"));
children.push(P("يهدف هذا المشروع إلى بناء تطبيق سطح مكتب لضغط ملفات الصوت، يتيح للمستخدم إدخال ملف صوتي وعرض خصائصه تلقائياً وتشغيله للمعاينة، ثم ضغطه باستخدام عدّة خوارزميات مختلفة مع إمكانية ضبط إعدادات الضغط قبل تنفيذه. كما يوفّر التطبيق مراقبة لحظية لأداء عملية الضغط عبر شريط تقدّم ورسوم بيانية، وإمكانية إلغاء العملية أو إعادة الضبط، وعرض تقرير تفصيلي بعد الانتهاء، وأخيراً حفظ الملف الناتج على القرص."));
children.push(P("وقد رُوعِيَ في التنفيذ فصلُ المسؤوليات ووضوحُ البنية البرمجية لتسهيل الصيانة والتوسعة، مع واجهة استخدام عصرية تدعم الوضعين الداكن والفاتح."));

// ---------- 2. الأدوات ----------
children.push(H1("2. الأدوات والتقنيات المستخدمة"));
children.push(bullet([ar("لغة البرمجة: "), en("C#")]));
children.push(bullet([ar("إطار العمل: "), en(".NET Framework 4.7.2"), ar(" مع "), en("Windows Forms (WinForms)")]));
children.push(bullet([ar("مكتبة الصوت: "), en("NAudio"), ar(" — لقراءة خصائص الملف الصوتي وتشغيله للمعاينة")]));
children.push(bullet([ar("الرسوم البيانية: "), en("System.Windows.Forms.DataVisualization"), ar(" (المضمّنة في "), en(".NET"), ar(") لعرض الأداء لحظياً")]));
children.push(bullet([ar("بيئة التطوير: "), en("Microsoft Visual Studio")]));

// ---------- 3. البنية ----------
children.push(H1("3. بنية التطبيق"));
children.push(P("اعتُمد نمط فصل المسؤوليات، حيث وُزِّع الكود على طبقات واضحة:"));
children.push(bullet([en("Models", { bold: true }), ar(": نماذج البيانات مثل "), en("CompressionSettings"), ar(" (الإعدادات) و"), en("AudioFileInfo"), ar(" (الخصائص).")]));
children.push(bullet([en("Services", { bold: true }), ar(": منطق العمل — قراءة الميتاداتا، تشغيل الصوت، وخوارزميات الضغط خلف الواجهة "), en("IAudioCompressionService"), ar(".")]));
children.push(bullet([en("UI", { bold: true }), ar(": أقسام الواجهة ("), en("FileSection, CompressionSection, MetadataSection, MonitorSection"), ar(") ونظام تصميم موحّد ونافذة التقرير.")]));
children.push(bullet([en("Form1", { bold: true }), ar(": المنسّق ("), en("Orchestrator"), ar(") الذي يربط الأقسام والأحداث ويدير عملية الضغط في الخلفية.")]));
children.push(spacer());
children.push(P([ar("الجدول التالي يلخّص أهم الملفات ودور كلٍّ منها:", { bold: true })]));
children.push(table([3200, 5826], [
  new TableRow({ tableHeader: true, children: [headCell("الملف", 3200), headCell("الدور", 5826)] }),
  new TableRow({ children: [cell([en("AudioMetadataService.cs")], 3200), cell("قراءة خصائص الملف الصوتي (الحجم، المدة، معدل العينات...)", 5826)] }),
  new TableRow({ children: [cell([en("AudioPlayerService.cs")], 3200), cell("تشغيل/إيقاف الملف للمعاينة قبل الضغط", 5826)] }),
  new TableRow({ children: [cell([en("DpcmCompressionService.cs")], 3200), cell("خوارزمية ترميز النبضة التفاضلي DPCM", 5826)] }),
  new TableRow({ children: [cell([en("DeltaCompressionService.cs")], 3200), cell("خوارزمية تعديل الدلتا Delta Modulation", 5826)] }),
  new TableRow({ children: [cell([en("NonlinearQuantizationService.cs")], 3200), cell("خوارزمية التكميم غير الخطي", 5826)] }),
  new TableRow({ children: [cell([en("CompressionSection.cs")], 3200), cell("اختيار الخوارزمية والإعدادات وإدارة خيط الضغط في الخلفية", 5826)] }),
  new TableRow({ children: [cell([en("MonitorSection.cs")], 3200), cell("الرسوم البيانية اللحظية (نسبة الضغط وسرعة المعالجة)", 5826)] }),
  new TableRow({ children: [cell([en("ReportForm.cs")], 3200), cell("نافذة التقرير بعد انتهاء الضغط", 5826)] }),
]));

// ---------- 4. الإجرائيات ----------
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(H1("4. الإجرائيات المتّبعة لتحقيق المتطلبات"));

children.push(H2("المتطلب 1: إدخال الملف الصوتي وعرضه (واجهة + سحب وإفلات)"));
children.push(bullet([ar("زر "), en("Load File"), ar(" يفتح نافذة اختيار ملف ("), en("OpenFileDialog"), ar(") تدعم "), en("WAV / MP3 / AAC"), ar(".")]));
children.push(bullet([ar("منطقة سحب وإفلات ("), en("Drag & Drop"), ar(") في "), en("FileSection"), ar(" تستقبل الملف عند إسقاطه عليها.")]));
children.push(bullet([ar("يُقرأ الملف إلى الذاكرة عبر "), en("File.ReadAllBytes"), ar(" ويُعرض اسمه ضمن مساحة العمل.")]));

children.push(H2("المتطلب 2: تشغيل الملف للمعاينة"));
children.push(bullet([ar("الصنف "), en("AudioPlayerService"), ar(" يستخدم "), en("NAudio (WaveOutEvent + AudioFileReader)"), ar(" لتشغيل/إيقاف الملف قبل تنفيذ الضغط.")]));

children.push(H2("المتطلب 3: عرض خصائص الملف تلقائياً"));
children.push(P([ar("عند التحميل يقرأ "), en("AudioMetadataService"), ar(" الخصائص ويعرضها في "), en("MetadataSection"), ar("، وتشمل:")]));
children.push(bullet("حجم الملف."));
children.push(bullet("المدة الزمنية."));
children.push(bullet([ar("معدل أخذ العينات ("), en("Sample Rate"), ar(").")]));
children.push(bullet([ar("عدد القنوات ("), en("Channels"), ar(").")]));
children.push(bullet([ar("معدل البت ("), en("Bit Rate"), ar(").")]));
children.push(bullet([ar("نوع الترميز ("), en("Codec"), ar(").")]));

children.push(H2("المتطلب 4: الضغط باستخدام ثلاث خوارزميات"));
children.push(P([ar("نُفِّذت ثلاث خوارزميات (تُشرح تفصيلاً في القسم 5): "), en("DPCM"), ar(" و"), en("Delta Modulation"), ar(" و"), en("Non-linear Quantization"), ar("، وتُختار من قائمة منسدلة، وكلٌّ منها يطبّق الواجهة الموحّدة "), en("IAudioCompressionService"), ar(".")]));

children.push(H2("المتطلب 5: فك ضغط الملف"));
children.push(bullet([ar("لكل خوارزمية دالة "), en("Decompress"), ar(" عكسية تعيد بناء العيّنات التقريبية من البيانات المضغوطة.")]));

children.push(H2("المتطلب 6: التحكم بإعدادات الضغط قبل التنفيذ"));
children.push(P([ar("تُضبط الإعدادات في "), en("CompressionSection"), ar(" وتُمرَّر عبر "), en("CompressionSettings"), ar(":")]));
children.push(bullet([ar("معدل أخذ العينات ("), en("Sample Rate"), ar("): من "), en("8000"), ar(" حتى "), en("48000 Hz"), ar(".")]));
children.push(bullet([ar("عدد مستويات التكميم ("), en("Quantization Levels"), ar("): من "), en("2"), ar(" حتى "), en("1024"), ar(".")]));
children.push(bullet("نوع الخوارزمية المستخدمة."));

children.push(H2("المتطلب 7: مراقبة الأداء أثناء التنفيذ (لحظياً)"));
children.push(bullet([ar("تنفيذ الضغط في خيط منفصل عبر "), en("BackgroundWorker"), ar(" حتى لا تتجمّد الواجهة.")]));
children.push(bullet([ar("شريط تقدّم ("), en("Progress Bar"), ar(") يوضّح نسبة الإنجاز، يُحدَّث نحو 100 مرّة أثناء المعالجة الفعلية.")]));
children.push(bullet([ar("رسمان بيانيان في "), en("MonitorSection"), ar(": الأول لنسبة الضغط، والثاني لسرعة المعالجة ("), en("MB/s"), ar(")، يتحدّثان لحظياً.")]));

children.push(H2("المتطلب 8: إلغاء عملية الضغط أثناء التنفيذ"));
children.push(bullet([ar("زر "), en("Cancel"), ar(" يستدعي "), en("Worker.CancelAsync"), ar("، والخوارزمية تفحص الإلغاء دورياً وتتوقّف بأمان.")]));

children.push(H2("المتطلب 9: إعادة ضبط القيم الأصلية"));
children.push(bullet([ar("زر "), en("Reset"), ar(" يعيد الحالة إلى الملف الأصلي ويمسح النتائج والرسوم البيانية.")]));

children.push(H2("المتطلب 10: عرض تقرير بعد انتهاء الضغط"));
children.push(P([ar("تظهر نافذة "), en("ReportForm"), ar(" تلقائياً وتحتوي على:")]));
children.push(bullet("حجم الملف قبل الضغط وبعده."));
children.push(bullet("نسبة التوفير في الحجم (%)."));
children.push(bullet([ar("الزمن المستغرق للعملية (يُقاس بـ "), en("Stopwatch"), ar(").")]));
children.push(bullet([ar("الخوارزمية المستخدمة وإعداداتها ("), en("Sample Rate, Sample Bit Rate"), ar(").")]));
children.push(bullet([ar("إمكانية حفظ التقرير كملف نصّي "), en(".txt"), ar(".")]));

children.push(H2("المتطلب 11: حفظ الملف الصوتي بعد الضغط"));
children.push(bullet([ar("زر "), en("Save"), ar(" يفتح "), en("SaveFileDialog"), ar(" ويكتب البيانات عبر "), en("File.WriteAllBytes"), ar(" بصيغة "), en("bin / wav"), ar(".")]));

// ---------- 5. الخوارزميات ----------
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(H1("5. شرح خوارزميات الضغط"));

children.push(H2("5.1 مفاهيم أساسية"));
children.push(P("الإشارة الصوتية الرقمية هي سلسلة من العيّنات (samples) تُؤخذ بمعدّل أخذ عينات محدّد، وتُكمَّم كل عيّنة إلى أحد مستويات محدودة. تعمل خوارزميات الضغط إمّا بتقليل عدد البتات المخصّصة لكل عيّنة، أو باستغلال الترابط الكبير بين العيّنات المتتالية (إذ غالباً ما تكون متقاربة القيم). في هذا التطبيق نتعامل مع العيّنات كقيم بطول 8 بت (من 0 إلى 255)."));
children.push(P("جميع الخوارزميات المطبّقة من نوع الضغط مع فقدان (Lossy)، أي أن الملف المُفكوك يكون مقارباً للأصل وليس مطابقاً له تماماً، وهو أمر مقبول في الصوت."));

children.push(H2("5.2 التكميم غير الخطي (Non-linear Quantization)"));
children.push(P("بدلاً من توزيع مستويات التكميم بالتساوي، تُطبَّق دالة غير خطية (Companding) تمنح دقّة أعلى للقيم المنخفضة حيث تكون الأذن البشرية أكثر حساسية."));
children.push(bullet([ar("الضغط: نُطبِّع العيّنة إلى المجال "), en("[0,1]"), ar("، ثم نطبّق جذراً تربيعياً "), en("(x^0.5)"), ar("، ونضربها بعدد المستويات. وعند عدد مستويات "), en("≤ 16"), ar(" نعبّئ عيّنتين في بايت واحد فتبلغ نسبة الضغط نحو "), en("2×"), ar(".")]));
children.push(bullet([ar("فك الضغط: العملية العكسية برفع القيمة إلى الأس "), en("2 (x^2)"), ar(".")]));
children.push(bullet([ar("المزايا: جودة أفضل عند الإشارات منخفضة السعة. العيوب: تشويه أكبر عند السعات العالية.")]));

children.push(H2("5.3 ترميز النبضة التفاضلي (DPCM)"));
children.push(P("بدلاً من ترميز قيمة كل عيّنة، نرمّز الفرق بينها وبين قيمة متنبَّأ بها (العيّنة السابقة)؛ ولأن الفروق بين العيّنات المتتالية صغيرة عادةً فإنها تحتاج عدد بتات أقل."));
children.push(bullet([ar("الضغط: "), en("diff = current - prev"), ar("، ثم نُكمّم الفرق إلى 4 بتات (16 مستوى، المجال "), en("[-8, 7]"), ar(") ونعبّئ عيّنتين في بايت → نسبة ضغط نحو "), en("2×"), ar(".")]));
children.push(bullet([ar("فك الضغط: نراكم الفروق المُكمَّمة على آخر قيمة مُعاد بناؤها.")]));
children.push(bullet([ar("الإعداد المؤثّر: عدد مستويات التكميم يحدّد خطوة التكميم "), en("step = 256 / levels"), ar(".")]));
children.push(bullet([ar("المزايا: بسيط وفعّال للإشارات المترابطة. العيوب: تراكم الخطأ، وقصور عند التغيّرات الحادّة.")]));

children.push(H2("5.4 تعديل الدلتا (Delta Modulation)"));
children.push(P("هو حالة خاصة من DPCM تستخدم بتاً واحداً فقط لكل عيّنة: نرفع القيمة المتنبَّأ بها أو نخفضها بمقدار خطوة ثابتة."));
children.push(bullet([ar("الضغط: البت = 1 إذا كانت العيّنة ≥ القيمة المتنبَّأ بها (فنزيدها بمقدار الخطوة)، وإلا 0 (فننقصها) → بت واحد لكل عيّنة → نسبة ضغط نحو "), en("8×"), ar(".")]));
children.push(bullet([ar("فك الضغط: نبدأ من قيمة ابتدائية ونتحرّك صعوداً/هبوطاً بمقدار الخطوة حسب البتات.")]));
children.push(bullet([ar("المزايا: أعلى نسبة ضغط وأبسط تنفيذ. العيوب: تحميل المنحدر ("), en("Slope Overload"), ar(") عند التغيّرات السريعة، وضجيج التحبّب.")]));

children.push(H2("5.5 جدول مقارنة بين الخوارزميات"));
children.push(table([2600, 1500, 1800, 3126], [
  new TableRow({ tableHeader: true, children: [headCell("الخوارزمية", 2600), headCell("بت/عيّنة", 1500), headCell("نسبة الضغط", 1800), headCell("الملاحظات", 3126)] }),
  new TableRow({ children: [cell("التكميم غير الخطي", 2600), cell("4 (عند ≤16)", 1500), cell("~2×", 1800), cell("شركة غير خطية تحسّن الجودة عند السعات المنخفضة", 3126)] }),
  new TableRow({ children: [cell([en("DPCM")], 2600), cell("4", 1500), cell("~2×", 1800), cell("ترميز الفروق بين العيّنات المتتالية", 3126)] }),
  new TableRow({ children: [cell("تعديل الدلتا", 2600), cell("1", 1500), cell("~8×", 1800), cell("الأبسط والأعلى ضغطاً والأكثر فقداناً", 3126)] }),
]));

// ---------- 6. سير العمل ----------
children.push(H1("6. واجهة الاستخدام وسير العمل"));
children.push(numItem("تحميل ملف صوتي عبر الزر أو السحب والإفلات."));
children.push(numItem("معاينة الملف بتشغيله."));
children.push(numItem("ضبط الخوارزمية وإعداداتها (معدل العينات، مستويات التكميم)."));
children.push(numItem([ar("الضغط عبر "), en("Compress"), ar(" مع مراقبة شريط التقدّم والرسوم البيانية.")]));
children.push(numItem("مراجعة التقرير الذي يظهر بعد الانتهاء."));
children.push(numItem("حفظ الملف الناتج، مع إمكانية فك الضغط أو إعادة الضبط أو الإلغاء في أي وقت."));

// ---------- 7. صور من التطبيق ----------
children.push(new Paragraph({ children: [new PageBreak()] }));
children.push(H1("7. صور من التطبيق"));
children.push(P("الواجهة الرئيسية للتطبيق في الوضع الداكن، وتظهر فيها أقسام إدارة الملف ومحرّك الضغط مع الإعدادات (الخوارزمية، مستويات التكميم، معدل العينات) وأزرار التنفيذ:"));
children.push(imagePara("shots/ui_top_c.png", 540));
children.push(caption("الشكل 1: الواجهة الرئيسية (إدارة الملف + محرّك الضغط)"));
children.push(P("أقسام البيانات الوصفية للملف والنتائج (الحجم الأصلي/الناتج ونسبة الضغط) وقسم مراقبة الأداء اللحظي بالرسوم البيانية:"));
children.push(imagePara("shots/ui_bottom_c.png", 540));
children.push(caption("الشكل 2: البيانات الوصفية والنتائج ومراقبة الأداء"));

// ---------- 8. خاتمة ----------
children.push(H1("8. الخاتمة"));
children.push(P("حقّق التطبيق متطلبات المشروع كاملةً: إدخال الملف وعرض خصائصه وتشغيله، والضغط بثلاث خوارزميات مع التحكم بالإعدادات، وفك الضغط، والمراقبة اللحظية للأداء عبر شريط التقدّم والرسوم البيانية، وإلغاء العملية وإعادة الضبط، وعرض تقرير تفصيلي، وحفظ الناتج على القرص. وقد أتاحت البنية المعتمدة على فصل المسؤوليات سهولة إضافة خوارزميات جديدة مستقبلاً عبر تطبيق الواجهة الموحّدة دون تعديل بقية أجزاء التطبيق."));

// =====================================================
// DOCUMENT
// =====================================================
const doc = new Document({
  styles: {
    default: { document: { run: { font: FONT, size: 24 } } },
    paragraphStyles: [
      {
        id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 32, bold: true, font: FONT, color: "1F3864" },
        paragraph: { spacing: { before: 260, after: 140 }, outlineLevel: 0 }
      },
      {
        id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 27, bold: true, font: FONT, color: "2E5496" },
        paragraph: { spacing: { before: 180, after: 100 }, outlineLevel: 1 }
      },
    ]
  },
  numbering: {
    config: [
      {
        reference: "bullets",
        levels: [{ level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }]
      },
      {
        reference: "numbers",
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }]
      },
    ]
  },
  sections: [{
    properties: {
      page: {
        size: { width: 11906, height: 16838 },
        margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 }
      }
    },
    children
  }]
});

Packer.toBuffer(doc).then(buffer => {
  const out = process.argv[2] || "report.docx";
  fs.writeFileSync(out, buffer);
  console.log("WROTE " + out + " (" + buffer.length + " bytes)");
});
