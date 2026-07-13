import { createContext, useContext, useState, useCallback } from "react";

const translations = {
  uz: {
    common: {
      loading: "Yuklanmoqda...",
      error: "Xatolik yuz berdi",
      save: "Saqlash",
      cancel: "Bekor qilish",
      delete: "O'chirish",
      search: "Qidirish",
      refresh: "Yangilash",
      back: "Orqaga",
      yes: "Ha",
      no: "Yo'q",
      success: "Muvaffaqiyatli",
      failed: "Muvaffaqiyatsiz",
      total: "Jami",
      amount: "Summa",
      status: "Holat",
      date: "Sana",
      actions: "Amallar",
      details: "Batafsil",
      filter: "Filtrlash",
      reset: "Tozalash",
      export: "Eksport",
      create: "Yaratish",
      edit: "Tahrirlash",
      noData: "Ma'lumot topilmadi",
      confirmDelete: "Rostdan ham o'chirmoqchimisiz?",
    },
    sidebar: {
      dashboard: "Dashboard",
      transactions: "Tranzaksiyalar",
      sverka: "Sverka",
      reports: "Hisobotlar",
      stations: "Zapravkalar",
      users: "Foydalanuvchilar",
      shareholders: "Ulushdorlar",
      testImitation: "Test/Imitation",
    },
    dashboard: {
      pageTitle: "Dashboard",
      totalTransactions: "Jami tranzaksiyalar",
      todayAmount: "Bugungi summa",
    },
    transactions: {
      pageTitle: "Tranzaksiyalar",
      tableHeaders: {
        id: "ID",
        contragent: "Kontragent",
        amount: "Summa",
        currency: "Valyuta",
        status: "Holat",
        date: "Sana",
        paymentSystem: "To'lov tizimi",
        station: "Zapravka",
        column: "Kalonka",
      },
      statusCompleted: "Bajarilgan",
      statusFailed: "Xatolik",
      statusProcessing: "Jarayonda",
      statusCreated: "Yaratilgan",
      statusRefunded: "Qaytarilgan",
    },
    sverka: {
      pageTitle: "Sverka",
      description:
        "Sverka — bu to'lov tizimlari bilan tranzaksiyalarni solishtirish va tekshirish bo'limi. Bu yerda siz kunlik, oylik va davriy hisobotlarni avtomatik ravishda solishtirishingiz mumkin.",
    },
    reports: {
      pageTitle: "Hisobotlar",
      description:
        "Hisobotlar bo'limida siz to'lov tranzaksiyalari bo'yicha batafsil hisobotlarni yaratishingiz, filtrlashingiz va eksport qilishingiz mumkin. Kunlik, haftalik va oylik hisobotlar mavjud.",
    },
    login: {
      title: "Tizimga kirish",
      username: "Foydalanuvchi nomi",
      password: "Parol",
      submit: "Kirish",
      loggingIn: "Kutilmoqda...",
      invalidCredentials: "Noto'g'ri foydalanuvchi nomi yoki parol",
    },
    topbar: {
      logout: "Chiqish",
      welcome: "Xush kelibsiz",
    },
    testImitation: {
      pageTitle: "Test / Imitation",
      description:
        "To'lov tizimlariga test so'rovlarini yuborish uchun imitatsiya paneli. Haqiqiy to'lov amalga oshirilmaydi.",
      selectProvider: "To'lov tizimini tanlang",
      enterAmount: "Summani kiriting",
      enterCarNumber: "Avtomobil raqami (ixtiyoriy)",
      submitButton: "Test to'lovni yuborish",
      submitting: "Yuborilmoqda...",
      transactionCreated: "Test tranzaksiya muvaffaqiyatli yaratildi!",
      providers: {
        uzcard: "Uzcard",
        humo: "Humo",
        click: "Click",
        payme: "Payme",
        apelsin: "Apelsin",
      },
      resultTitle: "Tranzaksiya natijasi",
      transactionId: "Tranzaksiya ID",
      provider: "To'lov tizimi",
      carNumberLabel: "Avtomobil raqami",
      createdAt: "Yaratilgan vaqt",
      testWarning:
        "⚠️ Bu test rejimi. Haqiqiy to'lov amalga oshirilmaydi. Test ma'lumotlar bilan ishlash uchun mo'ljallangan.",
    },
    stations: {
      pageTitle: "Zapravkalar",
      addStation: "Yangi zapravka qo'shish",
      editStation: "Zapravkani tahrirlash",
      name: "Nomi",
      address: "Manzil",
      region: "Hudud",
      phone: "Telefon",
      latitude: "Kenglik",
      longitude: "Uzunlik",
      columns: "Kalonkalar",
      columnsCount: "Kalonkalar soni",
      addColumn: "Kalonka qo'shish",
      editColumn: "Kalonkani tahrirlash",
      columnName: "Kalonka nomi",
      columnNumber: "Kalonka raqami",
      fuelType: "Yoqilg'i turi",
      pricePerLiter: "1 litr narxi",
      fuelTypes: {
        ai80: "AI-80",
        ai92: "AI-92",
        ai95: "AI-95",
        diesel: "Dizel",
        gas: "Gaz",
      },
      deleteStation: "Zapravkani o'chirish",
      deleteConfirm: "Rostdan ham zapravkani o'chirmoqchimisiz?",
      stationSaved: "Zapravka muvaffaqiyatli saqlandi",
      stationDeleted: "Zapravka o'chirildi",
      deleteColumn: "Kalonkani o'chirish",
      confirmDeleteColumn: "Rostdan ham ushbu kalonkani o'chirmoqchimisiz?",
      columnSaved: "Kalonka muvaffaqiyatli saqlandi",
      columnDeleted: "Kalonka o'chirildi",
      selectFuelType: "Yoqilg'i turini tanlang",
      active: "Faol",
      inactive: "Nofaol",
    },
    users: {
      pageTitle: "Foydalanuvchilar",
      addUser: "Yangi foydalanuvchi qo'shish",
      editUser: "Foydalanuvchini tahrirlash",
      username: "Foydalanuvchi nomi",
      fullName: "To'liq ism",
      email: "Email",
      password: "Parol",
      role: "Rol",
      lastLogin: "So'nggi kirish",
      deactivate: "Bloklash",
      activate: "Faollashtirish",
      roles: {
        SuperAdmin: "SuperAdmin",
        Admin: "Admin",
        Manager: "Manager",
        Operator: "Operator",
        Shareholder: "Shareholder",
        ReadOnly: "ReadOnly",
      },
    },
    shareholders: {
      pageTitle: "Ulushdorlar",
      addShareholder: "Yangi ulushdor qo'shish",
      editShareholder: "Ulushdorni tahrirlash",
      fullName: "To'liq ism",
      company: "Kompaniya",
      sharePercentage: "Ulush %",
      contractNumber: "Shartnoma №",
      contractDate: "Shartnoma sanasi",
      deleteShareholder: "Ulushdorni o'chirish",
    },
    payment: {
      pageTitle: "To'lov",
      station: "Zapravka",
      column: "Kalonka",
      fuelType: "Yoqilg'i turi",
      pricePerLiter: "1 litr narxi",
      enterAmount: "Summani kiriting",
      liters: "Litraj",
      selectProvider: "To'lov tizimi",
      carNumber: "Avto raqam",
      pay: "To'lash",
      paying: "To'lov amalga oshirilmoqda...",
      success: "To'lov muvaffaqiyatli!",
      transactionId: "Tranzaksiya ID",
      amount: "Summa",
      processedAt: "To'lov vaqti",
    },
  },

  ru: {
    common: {
      loading: "Загрузка...",
      error: "Произошла ошибка",
      save: "Сохранить",
      cancel: "Отмена",
      delete: "Удалить",
      search: "Поиск",
      refresh: "Обновить",
      back: "Назад",
      yes: "Да",
      no: "Нет",
      success: "Успешно",
      failed: "Ошибка",
      total: "Всего",
      amount: "Сумма",
      status: "Статус",
      date: "Дата",
      actions: "Действия",
      details: "Подробнее",
      filter: "Фильтр",
      reset: "Сброс",
      export: "Экспорт",
      create: "Создать",
      edit: "Редактировать",
      noData: "Нет данных",
      confirmDelete: "Вы уверены, что хотите удалить?",
    },
    sidebar: {
      dashboard: "Дашборд",
      transactions: "Транзакции",
      sverka: "Сверка",
      reports: "Отчёты",
      stations: "Заправки",
      users: "Пользователи",
      shareholders: "Акционеры",
      testImitation: "Тест/Имитация",
    },
    dashboard: {
      pageTitle: "Дашборд",
      totalTransactions: "Всего транзакций",
      todayAmount: "Сумма за сегодня",
    },
    transactions: {
      pageTitle: "Транзакции",
      tableHeaders: {
        id: "ID",
        contragent: "Контрагент",
        amount: "Сумма",
        currency: "Валюта",
        status: "Статус",
        date: "Дата",
        paymentSystem: "Платёжная система",
        station: "Заправка",
        column: "Колонка",
      },
      statusCompleted: "Завершено",
      statusFailed: "Ошибка",
      statusProcessing: "В обработке",
      statusCreated: "Создано",
      statusRefunded: "Возврат",
    },
    sverka: {
      pageTitle: "Сверка",
      description:
        "Сверка — это раздел для сравнения и проверки транзакций с платёжными системами. Здесь вы можете автоматически сверять ежедневные, ежемесячные и периодические отчёты.",
    },
    reports: {
      pageTitle: "Отчёты",
      description:
        "В разделе отчётов вы можете создавать, фильтровать и экспортировать подробные отчёты по платёжным транзакциям. Доступны ежедневные, еженедельные и ежемесячные отчёты.",
    },
    login: {
      title: "Вход в систему",
      username: "Имя пользователя",
      password: "Пароль",
      submit: "Войти",
      loggingIn: "Ожидание...",
      invalidCredentials: "Неверное имя пользователя или пароль",
    },
    topbar: {
      logout: "Выход",
      welcome: "Добро пожаловать",
    },
    testImitation: {
      pageTitle: "Тест / Имитация",
      description:
        "Панель имитации для отправки тестовых запросов в платёжные системы. Реальные платежи не выполняются.",
      selectProvider: "Выберите платёжную систему",
      enterAmount: "Введите сумму",
      enterCarNumber: "Номер автомобиля (необязательно)",
      submitButton: "Отправить тестовый платёж",
      submitting: "Отправка...",
      transactionCreated: "Тестовая транзакция успешно создана!",
      providers: {
        uzcard: "Uzcard",
        humo: "Humo",
        click: "Click",
        payme: "Payme",
        apelsin: "Apelsin",
      },
      resultTitle: "Результат транзакции",
      transactionId: "ID транзакции",
      provider: "Платёжная система",
      carNumberLabel: "Номер автомобиля",
      createdAt: "Дата создания",
      testWarning:
        "⚠️ Это тестовый режим. Реальные платежи не выполняются. Предназначено для работы с тестовыми данными.",
    },
    stations: {
      pageTitle: "Заправки",
      addStation: "Добавить заправку",
      editStation: "Редактировать заправку",
      name: "Название",
      address: "Адрес",
      region: "Регион",
      phone: "Телефон",
      latitude: "Широта",
      longitude: "Долгота",
      columns: "Колонки",
      columnsCount: "Количество колонок",
      addColumn: "Добавить колонку",
      editColumn: "Редактировать колонку",
      columnName: "Название колонки",
      columnNumber: "Номер колонки",
      fuelType: "Тип топлива",
      pricePerLiter: "Цена за литр",
      fuelTypes: {
        ai80: "AI-80",
        ai92: "AI-92",
        ai95: "AI-95",
        diesel: "Дизель",
        gas: "Газ",
      },
      deleteStation: "Удалить заправку",
      deleteConfirm: "Вы уверены, что хотите удалить заправку?",
      stationSaved: "Заправка успешно сохранена",
      stationDeleted: "Заправка удалена",
      deleteColumn: "Удалить колонку",
      confirmDeleteColumn: "Вы уверены, что хотите удалить эту колонку?",
      columnSaved: "Колонка успешно сохранена",
      columnDeleted: "Колонка удалена",
      selectFuelType: "Выберите тип топлива",
      active: "Активен",
      inactive: "Неактивен",
    },
    users: {
      pageTitle: "Пользователи",
      addUser: "Добавить пользователя",
      editUser: "Редактировать пользователя",
      username: "Имя пользователя",
      fullName: "Полное имя",
      email: "Email",
      password: "Пароль",
      role: "Роль",
      lastLogin: "Последний вход",
      deactivate: "Блокировать",
      activate: "Активировать",
      roles: {
        SuperAdmin: "SuperAdmin",
        Admin: "Admin",
        Manager: "Manager",
        Operator: "Operator",
        Shareholder: "Shareholder",
        ReadOnly: "ReadOnly",
      },
    },
    shareholders: {
      pageTitle: "Акционеры",
      addShareholder: "Добавить акционера",
      editShareholder: "Редактировать акционера",
      fullName: "Полное имя",
      company: "Компания",
      sharePercentage: "Доля %",
      contractNumber: "№ договора",
      contractDate: "Дата договора",
      deleteShareholder: "Удалить акционера",
    },
    payment: {
      pageTitle: "Оплата",
      station: "Заправка",
      column: "Колонка",
      fuelType: "Тип топлива",
      pricePerLiter: "Цена за литр",
      enterAmount: "Введите сумму",
      liters: "Литраж",
      selectProvider: "Платёжная система",
      carNumber: "Номер авто",
      pay: "Оплатить",
      paying: "Оплата выполняется...",
      success: "Оплата успешна!",
      transactionId: "ID транзакции",
      amount: "Сумма",
      processedAt: "Время оплаты",
    },
  },
};

const LanguageContext = createContext(null);

function LanguageProvider({ children }) {
  const [lang, setLang] = useState(() => {
    try {
      return localStorage.getItem("gzs_billing_lang") || "uz";
    } catch {
      return "uz";
    }
  });

  const toggleLanguage = useCallback(() => {
    setLang((prev) => {
      const next = prev === "uz" ? "ru" : "uz";
      try {
        localStorage.setItem("gzs_billing_lang", next);
      } catch {
        // ignore storage errors
      }
      return next;
    });
  }, []);

  const t = useCallback(
    (key) => {
      const keys = key.split(".");
      let value = translations[lang];
      for (const k of keys) {
        if (value && typeof value === "object") {
          value = value[k];
        } else {
          return key;
        }
      }
      return value || key;
    },
    [lang],
  );

  const value = {
    lang,
    setLang,
    toggleLanguage,
    t,
  };

  return (
    <LanguageContext.Provider value={value}>
      {children}
    </LanguageContext.Provider>
  );
}

function useTranslation() {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error("useTranslation must be used within a LanguageProvider");
  }
  return context;
}

export { LanguageProvider, useTranslation };
