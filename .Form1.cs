using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CinemaApp
{
    public partial class Form1 : Form
    {
        private string dbPath = "cinema.db";
        private string connectionString;
        private TabControl mainTabControl;
        private FlowLayoutPanel moviesPanel;
        private FlowLayoutPanel newsPanel;
        private DataGridView ticketsGridView;
        private FlowLayoutPanel employeesPanel;
        private FlowLayoutPanel clientsPanel;
        private Label totalSumLabel;
        private Button deleteTicketBtn;
        private Label protectionStatusLabel;

        private Random random = new Random();

        public Form1()
        {
            // Простая защита: запрос пароля при запуске
            if (!CheckPassword())
            {
                Environment.Exit(0);
            }

            connectionString = $"Data Source={dbPath};Version=3;";
            CreateDatabase();
            InitializeInterface();
            LoadMoviesInRandomOrder();
            LoadNews();
            LoadTickets();
            LoadEmployees();
            LoadRegularClients();
        }

        private bool CheckPassword()
        {
            Form passwordForm = new Form();
            passwordForm.Text = "Вход в систему";
            passwordForm.Size = new Size(350, 160);
            passwordForm.StartPosition = FormStartPosition.CenterScreen;
            passwordForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            passwordForm.MaximizeBox = false;
            passwordForm.BackColor = Color.FromArgb(30, 30, 40);

            Label promptLabel = new Label();
            promptLabel.Text = "Введите пароль для доступа к системе:";
            promptLabel.ForeColor = Color.White;
            promptLabel.Font = new Font("Segoe UI", 10);
            promptLabel.Location = new Point(20, 20);
            promptLabel.Size = new Size(290, 30);
            passwordForm.Controls.Add(promptLabel);

            TextBox passwordBox = new TextBox();
            passwordBox.Location = new Point(20, 55);
            passwordBox.Size = new Size(290, 25);
            passwordBox.PasswordChar = '*';
            passwordBox.Font = new Font("Segoe UI", 10);
            passwordForm.Controls.Add(passwordBox);

            Button okBtn = new Button();
            okBtn.Text = "Войти";
            okBtn.Size = new Size(100, 35);
            okBtn.Location = new Point(110, 95);
            okBtn.BackColor = Color.FromArgb(0, 80, 200);
            okBtn.ForeColor = Color.White;
            okBtn.FlatStyle = FlatStyle.Flat;
            okBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            passwordForm.Controls.Add(okBtn);

            bool result = false;
            okBtn.Click += (s, e) =>
            {
                if (passwordBox.Text == "D1234") // новый пароль
                {
                    result = true;
                    passwordForm.Close();
                }
                else
                {
                    MessageBox.Show("Неверный пароль! Доступ запрещён.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            passwordForm.ShowDialog();
            return result;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle rect = this.ClientRectangle;
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(20, 20, 30), Color.FromArgb(35, 35, 45), 90f))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        private void CreateDatabase()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                new SQLiteCommand("DROP TABLE IF EXISTS seats", conn).ExecuteNonQuery();
                new SQLiteCommand("DROP TABLE IF EXISTS tickets", conn).ExecuteNonQuery();
                new SQLiteCommand("DROP TABLE IF EXISTS movies", conn).ExecuteNonQuery();
                new SQLiteCommand("DROP TABLE IF EXISTS news", conn).ExecuteNonQuery();
                new SQLiteCommand("DROP TABLE IF EXISTS contacts", conn).ExecuteNonQuery();
                new SQLiteCommand("DROP TABLE IF EXISTS employees", conn).ExecuteNonQuery();
                new SQLiteCommand("DROP TABLE IF EXISTS regular_clients", conn).ExecuteNonQuery();

                string sqlMovies = @"
                    CREATE TABLE movies (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT NOT NULL,
                        genre TEXT,
                        duration INTEGER,
                        poster_color TEXT,
                        description TEXT,
                        price REAL DEFAULT 400,
                        release_start TEXT,
                        release_end TEXT
                    )";
                new SQLiteCommand(sqlMovies, conn).ExecuteNonQuery();

                string sqlSeats = @"
                    CREATE TABLE seats (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        movie_id INTEGER NOT NULL,
                        row_num INTEGER NOT NULL,
                        seat_num INTEGER NOT NULL,
                        is_free INTEGER NOT NULL DEFAULT 1
                    )";
                new SQLiteCommand(sqlSeats, conn).ExecuteNonQuery();

                string sqlTickets = @"
                    CREATE TABLE tickets (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        movie_id INTEGER NOT NULL,
                        movie_title TEXT NOT NULL,
                        row_num INTEGER NOT NULL,
                        seat_num INTEGER NOT NULL,
                        price REAL NOT NULL,
                        booking_date TEXT NOT NULL,
                        ticket_code TEXT NOT NULL,
                        client_name TEXT
                    )";
                new SQLiteCommand(sqlTickets, conn).ExecuteNonQuery();

                string sqlNews = @"
                    CREATE TABLE news (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT NOT NULL,
                        content TEXT NOT NULL,
                        publish_date TEXT NOT NULL
                    )";
                new SQLiteCommand(sqlNews, conn).ExecuteNonQuery();

                string sqlContacts = @"
                    CREATE TABLE contacts (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        phone TEXT,
                        email TEXT,
                        address TEXT,
                        work_time TEXT,
                        instagram TEXT,
                        telegram TEXT
                    )";
                new SQLiteCommand(sqlContacts, conn).ExecuteNonQuery();

                string sqlEmployees = @"
                    CREATE TABLE employees (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        position TEXT NOT NULL,
                        phone TEXT,
                        salary REAL DEFAULT 0,
                        hire_date TEXT
                    )";
                new SQLiteCommand(sqlEmployees, conn).ExecuteNonQuery();

                string sqlRegularClients = @"
                    CREATE TABLE regular_clients (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        last_visit TEXT NOT NULL,
                        visit_count INTEGER DEFAULT 1
                    )";
                new SQLiteCommand(sqlRegularClients, conn).ExecuteNonQuery();

                // Обновлённые даты фильмов - 2026 год, рандомно
                string[] randomStarts = { "2026-01-15", "2026-02-10", "2026-03-05", "2026-04-20", "2026-05-01", "2026-06-15", "2026-07-10", "2026-08-25", "2026-09-12", "2026-10-01", "2026-11-20", "2026-12-05" };
                string[] randomEnds = { "2026-03-20", "2026-04-15", "2026-05-10", "2026-06-25", "2026-07-20", "2026-08-30", "2026-09-25", "2026-10-20", "2026-11-15", "2026-12-10", "2027-01-15", "2027-02-01" };

                var movies = new[]
                {
                    new { title = "Аватар 3", genre = "Фантастика", duration = 192, color = "#4B0082", desc = "Эпическое приключение на Пандоре", price = 450, start = randomStarts[0], end = randomEnds[0] },
                    new { title = "Матрица 4", genre = "Боевик", duration = 148, color = "#006400", desc = "Продолжение легендарной саги", price = 420, start = randomStarts[1], end = randomEnds[1] },
                    new { title = "Бэтмен", genre = "Детектив", duration = 176, color = "#1C1C1C", desc = "Тёмный рыцарь возвращается", price = 480, start = randomStarts[2], end = randomEnds[2] },
                    new { title = "Дюна 2", genre = "Фантастика", duration = 166, color = "#8B4513", desc = "Продолжение эпической саги", price = 500, start = randomStarts[3], end = randomEnds[3] },
                    new { title = "Гладиатор 2", genre = "Исторический", duration = 170, color = "#8B0000", desc = "Возвращение легенды", price = 520, start = randomStarts[4], end = randomEnds[4] },
                    new { title = "Барби", genre = "Комедия", duration = 114, color = "#FF69B4", desc = "Яркая комедия о кукле Барби", price = 380, start = randomStarts[5], end = randomEnds[5] },
                    new { title = "Оппенгеймер", genre = "Драма", duration = 180, color = "#2F4F4F", desc = "История создателя атомной бомбы", price = 550, start = randomStarts[6], end = randomEnds[6] },
                    new { title = "Джон Уик 4", genre = "Боевик", duration = 169, color = "#191970", desc = "Неудержимый киллер возвращается", price = 490, start = randomStarts[7], end = randomEnds[7] },
                    new { title = "Человек-паук: Паутина вселенных", genre = "Мультфильм", duration = 140, color = "#DC143C", desc = "Новое приключение Человека-паука", price = 430, start = randomStarts[8], end = randomEnds[8] },
                    new { title = "Фуриоса", genre = "Боевик", duration = 148, color = "#B22222", desc = "Приквел Безумного Макса", price = 470, start = randomStarts[9], end = randomEnds[9] },
                    new { title = "Головоломка 2", genre = "Мультфильм", duration = 100, color = "#FFD700", desc = "Новые эмоции в голове", price = 390, start = randomStarts[10], end = randomEnds[10] },
                    new { title = "Тихое место: День первый", genre = "Ужасы", duration = 99, color = "#36454F", desc = "Как всё начиналось", price = 410, start = randomStarts[11], end = randomEnds[11] }
                };

                using (var movieCmd = new SQLiteCommand("INSERT INTO movies (title, genre, duration, poster_color, description, price, release_start, release_end) VALUES (@t, @g, @d, @c, @desc, @price, @start, @end)", conn))
                {
                    foreach (var m in movies)
                    {
                        movieCmd.Parameters.Clear();
                        movieCmd.Parameters.AddWithValue("@t", m.title);
                        movieCmd.Parameters.AddWithValue("@g", m.genre);
                        movieCmd.Parameters.AddWithValue("@d", m.duration);
                        movieCmd.Parameters.AddWithValue("@c", m.color);
                        movieCmd.Parameters.AddWithValue("@desc", m.desc);
                        movieCmd.Parameters.AddWithValue("@price", m.price);
                        movieCmd.Parameters.AddWithValue("@start", m.start);
                        movieCmd.Parameters.AddWithValue("@end", m.end);
                        movieCmd.ExecuteNonQuery();
                    }
                }

                for (int movieId = 1; movieId <= 12; movieId++)
                {
                    for (int r = 1; r <= 6; r++)
                    {
                        for (int s = 1; s <= 8; s++)
                        {
                            var seatCmd = new SQLiteCommand("INSERT INTO seats (movie_id, row_num, seat_num, is_free) VALUES (@mid, @r, @s, 1)", conn);
                            seatCmd.Parameters.AddWithValue("@mid", movieId);
                            seatCmd.Parameters.AddWithValue("@r", r);
                            seatCmd.Parameters.AddWithValue("@s", s);
                            seatCmd.ExecuteNonQuery();
                        }
                    }
                }

                var defaultNews = new[]
                {
                    new { title = "Открытие нового IMAX зала!", content = "Мы рады сообщить об открытии кинозала с технологией IMAX!", date = DateTime.Now.ToString("yyyy-MM-dd") },
                    new { title = "Скидка на попкорн 50%", content = "При покупке двух билетов попкорн бесплатно!", date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") },
                    new { title = "Встреча с режиссёром", content = "Вход свободный по билетам на любой сеанс", date = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd") },
                    new { title = "Ночные показы", content = "По субботам сеансы после 22:00 со скидкой 30%", date = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd") },
                    new { title = "Кинотеатр года", content = "Наш кинотеатр признан лучшим в городе!", date = DateTime.Now.AddDays(-4).ToString("yyyy-MM-dd") }
                };

                foreach (var n in defaultNews)
                {
                    var newsCmd = new SQLiteCommand("INSERT INTO news (title, content, publish_date) VALUES (@t, @c, @date)", conn);
                    newsCmd.Parameters.AddWithValue("@t", n.title);
                    newsCmd.Parameters.AddWithValue("@c", n.content);
                    newsCmd.Parameters.AddWithValue("@date", n.date);
                    newsCmd.ExecuteNonQuery();
                }

                var contactCmd = new SQLiteCommand("INSERT INTO contacts (phone, email, address, work_time, instagram, telegram) VALUES (@p, @e, @a, @wt, @inst, @tg)", conn);
                contactCmd.Parameters.AddWithValue("@p", "+7 (999) 123-45-67");
                contactCmd.Parameters.AddWithValue("@e", "info@cinema.ru");
                contactCmd.Parameters.AddWithValue("@a", "г. Москва, ул. Кино, д. 10");
                contactCmd.Parameters.AddWithValue("@wt", "Ежедневно 09:00 - 23:00");
                contactCmd.Parameters.AddWithValue("@inst", "@cinema_official");
                contactCmd.Parameters.AddWithValue("@tg", "@cinema_bot");
                contactCmd.ExecuteNonQuery();

                string[] employees = new string[]
                {
                    "Иванов Иван", "Петрова Анна", "Сидоров Алексей", "Козлова Мария", "Смирнов Дмитрий",
                    "Кузнецова Елена", "Попов Андрей", "Васильева Ольга", "Соколов Михаил", "Михайлова Татьяна",
                    "Новиков Павел", "Федорова Екатерина", "Морозов Владимир", "Волкова Наталья", "Алексеев Артем",
                    "Лебедева Ирина", "Семенов Денис", "Егорова Юлия", "Павлов Николай", "Ильина Анастасия"
                };
                string[] positions = new string[] { "Управляющий", "Кассир", "Администратор", "Билетёр", "Уборщик", "Охранник", "Продавец", "Бариста", "Менеджер", "Техник" };

                for (int i = 0; i < employees.Length; i++)
                {
                    var empCmd = new SQLiteCommand("INSERT INTO employees (name, position, phone, salary, hire_date) VALUES (@n, @p, @ph, @s, @date)", conn);
                    empCmd.Parameters.AddWithValue("@n", employees[i]);
                    empCmd.Parameters.AddWithValue("@p", positions[i % positions.Length]);
                    empCmd.Parameters.AddWithValue("@ph", $"+7 (999) {100 + i:000}-{(i * 13) % 100:00}-{(i * 27) % 100:00}");
                    empCmd.Parameters.AddWithValue("@s", 35000 + i * 1500);
                    empCmd.Parameters.AddWithValue("@date", $"2024-{i % 12 + 1:00}-{(i % 28 + 1):00}");
                    empCmd.ExecuteNonQuery();
                }

                // Добавляем постоянных клиентов
                string[] clients = new string[] { "Алексей Смирнов", "Мария Иванова", "Дмитрий Петров", "Елена Сидорова", "Анна Козлова" };
                for (int i = 0; i < clients.Length; i++)
                {
                    var clientCmd = new SQLiteCommand("INSERT INTO regular_clients (name, last_visit, visit_count) VALUES (@n, @lv, @vc)", conn);
                    clientCmd.Parameters.AddWithValue("@n", clients[i]);
                    clientCmd.Parameters.AddWithValue("@lv", $"2026-{random.Next(1, 13):00}-{random.Next(1, 28):00}");
                    clientCmd.Parameters.AddWithValue("@vc", random.Next(3, 50));
                    clientCmd.ExecuteNonQuery();
                }
            }
        }

        private void InitializeInterface()
        {
            this.Text = "🎬 КИНОТЕАТР";
            this.Size = new Size(1500, 950);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 30);

            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            mainTabControl.SizeMode = TabSizeMode.Fixed;
            mainTabControl.ItemSize = new Size(140, 55);
            mainTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            mainTabControl.DrawItem += DrawTab;
            this.Controls.Add(mainTabControl);

            // ========== ВКЛАДКА 1: АФИША ==========
            TabPage pageMovies = new TabPage("  🎬 АФИША  ");
            pageMovies.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageMovies);

            moviesPanel = new FlowLayoutPanel();
            moviesPanel.Dock = DockStyle.Fill;
            moviesPanel.AutoScroll = true;
            moviesPanel.Padding = new Padding(20);
            moviesPanel.BackColor = Color.FromArgb(30, 30, 40);
            pageMovies.Controls.Add(moviesPanel);

            // ========== ВКЛАДКА 2: НОВОСТИ ==========
            TabPage pageNews = new TabPage("  📰 НОВОСТИ  ");
            pageNews.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageNews);

            newsPanel = new FlowLayoutPanel();
            newsPanel.Dock = DockStyle.Fill;
            newsPanel.AutoScroll = true;
            newsPanel.Padding = new Padding(20);
            newsPanel.BackColor = Color.FromArgb(30, 30, 40);
            pageNews.Controls.Add(newsPanel);

            // ========== ВКЛАДКА 3: КОНТАКТЫ ==========
            TabPage pageContacts = new TabPage("  📞 КОНТАКТЫ  ");
            pageContacts.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageContacts);

            Panel contactsPanel = new Panel();
            contactsPanel.Dock = DockStyle.Fill;
            contactsPanel.BackColor = Color.FromArgb(40, 40, 50);
            contactsPanel.Padding = new Padding(60);
            pageContacts.Controls.Add(contactsPanel);

            Panel contactCard = new Panel();
            contactCard.Size = new Size(700, 500);
            contactCard.Location = new Point(250, 80);
            contactCard.BackColor = Color.FromArgb(50, 50, 65);
            contactCard.BorderStyle = BorderStyle.FixedSingle;
            contactsPanel.Controls.Add(contactCard);

            string[,] contacts = {
                {"📞 Телефон:", "+7 (999) 123-45-67"},
                {"✉️ E-mail:", "info@cinema.ru"},
                {"📍 Адрес:", "г. Москва, ул. Кино, д. 10"},
                {"⏰ Режим работы:", "Ежедневно 09:00 - 23:00"},
                {"📷 Instagram:", "@cinema_official"},
                {"💬 Telegram:", "@cinema_bot"}
            };

            for (int i = 0; i < 6; i++)
            {
                Label iconLabel = new Label();
                iconLabel.Text = contacts[i, 0];
                iconLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                iconLabel.ForeColor = Color.FromArgb(100, 150, 255);
                iconLabel.Location = new Point(50, 70 + i * 60);
                iconLabel.Size = new Size(160, 35);
                contactCard.Controls.Add(iconLabel);

                Label valueLabel = new Label();
                valueLabel.Text = contacts[i, 1];
                valueLabel.Font = new Font("Segoe UI", 12);
                valueLabel.ForeColor = Color.White;
                valueLabel.Location = new Point(230, 70 + i * 60);
                valueLabel.Size = new Size(400, 35);
                contactCard.Controls.Add(valueLabel);
            }

            // ========== ВКЛАДКА 4: БИЛЕТЫ ==========
            TabPage pageTickets = new TabPage("  🎫 БИЛЕТЫ  ");
            pageTickets.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageTickets);

            Panel topSpacer = new Panel();
            topSpacer.Dock = DockStyle.Top;
            topSpacer.Height = 20;
            topSpacer.BackColor = Color.FromArgb(30, 30, 40);
            pageTickets.Controls.Add(topSpacer);

            ticketsGridView = new DataGridView();
            ticketsGridView.Dock = DockStyle.Fill;
            ticketsGridView.BackgroundColor = Color.FromArgb(45, 45, 55);
            ticketsGridView.ForeColor = Color.White;
            ticketsGridView.GridColor = Color.FromArgb(70, 70, 80);
            ticketsGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ticketsGridView.RowHeadersVisible = false;
            ticketsGridView.AllowUserToAddRows = false;
            ticketsGridView.BorderStyle = BorderStyle.None;
            ticketsGridView.Font = new Font("Segoe UI", 11);
            ticketsGridView.RowTemplate.Height = 40;
            ticketsGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(55, 55, 65);
            ticketsGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 60, 160);
            ticketsGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            ticketsGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            ticketsGridView.ColumnHeadersHeight = 45;
            ticketsGridView.DefaultCellStyle.BackColor = Color.FromArgb(50, 50, 60);
            ticketsGridView.DefaultCellStyle.ForeColor = Color.White;
            ticketsGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 80, 200);
            ticketsGridView.DefaultCellStyle.SelectionForeColor = Color.White;

            ticketsGridView.ReadOnly = false;
            ticketsGridView.EditMode = DataGridViewEditMode.EditOnEnter;

            pageTickets.Controls.Add(ticketsGridView);

            Panel ticketBottomPanel = new Panel();
            ticketBottomPanel.Dock = DockStyle.Bottom;
            ticketBottomPanel.Height = 65;
            ticketBottomPanel.BackColor = Color.FromArgb(35, 35, 45);
            ticketBottomPanel.Padding = new Padding(15, 8, 15, 8);
            pageTickets.Controls.Add(ticketBottomPanel);

            totalSumLabel = new Label();
            totalSumLabel.Text = "💰 Общая сумма: 0 ₽";
            totalSumLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            totalSumLabel.ForeColor = Color.FromArgb(0, 200, 100);
            totalSumLabel.Location = new Point(15, 8);
            totalSumLabel.Size = new Size(280, 45);
            totalSumLabel.TextAlign = ContentAlignment.MiddleLeft;
            ticketBottomPanel.Controls.Add(totalSumLabel);

            Label hintLabel = new Label();
            hintLabel.Text = "✓ Отметьте билеты для удаления";
            hintLabel.Font = new Font("Segoe UI", 10);
            hintLabel.ForeColor = Color.LightGray;
            hintLabel.Location = new Point(300, 18);
            hintLabel.Size = new Size(200, 30);
            ticketBottomPanel.Controls.Add(hintLabel);

            deleteTicketBtn = new Button();
            deleteTicketBtn.Text = "🗑 УДАЛИТЬ ВЫБРАННЫЕ";
            deleteTicketBtn.Size = new Size(200, 48);
            deleteTicketBtn.Location = new Point(850, 8);
            deleteTicketBtn.BackColor = Color.FromArgb(200, 50, 50);
            deleteTicketBtn.ForeColor = Color.White;
            deleteTicketBtn.FlatStyle = FlatStyle.Flat;
            deleteTicketBtn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            deleteTicketBtn.Cursor = Cursors.Hand;
            deleteTicketBtn.Click += DeleteSelectedTickets;
            ticketBottomPanel.Controls.Add(deleteTicketBtn);

            Button refreshTicketsBtn = new Button();
            refreshTicketsBtn.Text = "🔄 ОБНОВИТЬ";
            refreshTicketsBtn.Size = new Size(160, 48);
            refreshTicketsBtn.Location = new Point(1070, 8);
            refreshTicketsBtn.BackColor = Color.FromArgb(0, 80, 200);
            refreshTicketsBtn.ForeColor = Color.White;
            refreshTicketsBtn.FlatStyle = FlatStyle.Flat;
            refreshTicketsBtn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            refreshTicketsBtn.Cursor = Cursors.Hand;
            refreshTicketsBtn.Click += (s, e) => LoadTickets();
            ticketBottomPanel.Controls.Add(refreshTicketsBtn);

            // ========== ВКЛАДКА 5: СОТРУДНИКИ ==========
            TabPage pageEmployees = new TabPage("  👥 СОТРУДНИКИ  ");
            pageEmployees.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageEmployees);

            employeesPanel = new FlowLayoutPanel();
            employeesPanel.Dock = DockStyle.Fill;
            employeesPanel.AutoScroll = true;
            employeesPanel.Padding = new Padding(20);
            employeesPanel.BackColor = Color.FromArgb(30, 30, 40);
            pageEmployees.Controls.Add(employeesPanel);

            // ========== ВКЛАДКА 6: ПОСТОЯННЫЕ КЛИЕНТЫ ==========
            TabPage pageClients = new TabPage("  👥 ПОСТОЯННЫЕ КЛИЕНТЫ  ");
            pageClients.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageClients);

            clientsPanel = new FlowLayoutPanel();
            clientsPanel.Dock = DockStyle.Fill;
            clientsPanel.AutoScroll = true;
            clientsPanel.Padding = new Padding(20);
            clientsPanel.BackColor = Color.FromArgb(30, 30, 40);
            pageClients.Controls.Add(clientsPanel);

            // ========== ВКЛАДКА 7: РЕЦЕНЗИРОВАНИЕ ==========
            TabPage pageReview = new TabPage("  🛡️ РЕЦЕНЗИРОВАНИЕ  ");
            pageReview.BackColor = Color.FromArgb(30, 30, 40);
            mainTabControl.TabPages.Add(pageReview);

            Panel reviewPanel = new Panel();
            reviewPanel.Dock = DockStyle.Fill;
            reviewPanel.BackColor = Color.FromArgb(40, 40, 50);
            reviewPanel.Padding = new Padding(60);
            pageReview.Controls.Add(reviewPanel);

            // Увеличил размер карточки, чтобы всё помещалось без наложений
            Panel reviewCard = new Panel();
            reviewCard.Size = new Size(900, 520);
            reviewCard.Location = new Point(150, 60);
            reviewCard.BackColor = Color.FromArgb(50, 50, 65);
            reviewCard.BorderStyle = BorderStyle.FixedSingle;
            reviewPanel.Controls.Add(reviewCard);

            // Иконка разработчика
            Label devIcon = new Label();
            devIcon.Text = "👨‍💻";
            devIcon.Font = new Font("Segoe UI Emoji", 48, FontStyle.Bold);
            devIcon.ForeColor = Color.FromArgb(100, 150, 255);
            devIcon.Size = new Size(100, 80);
            devIcon.Location = new Point(400, 20);
            devIcon.TextAlign = ContentAlignment.MiddleCenter;
            reviewCard.Controls.Add(devIcon);

            // Заголовок
            Label reviewHeader = new Label();
            reviewHeader.Text = "ИНФОРМАЦИЯ О РАЗРАБОТЧИКЕ";
            reviewHeader.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            reviewHeader.ForeColor = Color.Gold;
            reviewHeader.Size = new Size(800, 40);
            reviewHeader.Location = new Point(50, 100);
            reviewHeader.TextAlign = ContentAlignment.MiddleCenter;
            reviewCard.Controls.Add(reviewHeader);

            // Разделитель
            Panel reviewSeparator = new Panel();
            reviewSeparator.Size = new Size(800, 2);
            reviewSeparator.Location = new Point(50, 145);
            reviewSeparator.BackColor = Color.Gold;
            reviewCard.Controls.Add(reviewSeparator);

            // Информация о разработчике
            string[,] developerInfo = {
                {"📌 Фамилия:", "Бурбело"},
                {"📌 Имя:", "Даниил"},
                {"📌 Отчество:", "Иванович"},
                {"🎓 Учебное заведение:", "ДГУ (филиал в г. Кизляр)"},
                {"📚 Специальность:", "09.02.07 Информационные системы и программирование"},
                {"📅 Курс:", "2-й курс"},
                {"🛡️ Статус защиты:", "Курсовая работа защищена"}
            };

            for (int i = 0; i < developerInfo.GetLength(0); i++)
            {
                Label fieldLabel = new Label();
                fieldLabel.Text = developerInfo[i, 0];
                fieldLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                fieldLabel.ForeColor = Color.FromArgb(100, 150, 255);
                fieldLabel.Size = new Size(200, 38);
                fieldLabel.Location = new Point(60, 170 + i * 48);
                reviewCard.Controls.Add(fieldLabel);

                Label valueLabel = new Label();
                valueLabel.Text = developerInfo[i, 1];
                valueLabel.Font = new Font("Segoe UI", 12);
                valueLabel.ForeColor = Color.White;
                valueLabel.Size = new Size(500, 38);
                valueLabel.Location = new Point(290, 170 + i * 48);
                reviewCard.Controls.Add(valueLabel);
            }

            // Статус защиты (светящаяся метка)
            protectionStatusLabel = new Label();
            protectionStatusLabel.Text = "🔒 ПРОГРАММА ЗАЩИЩЕНА 🔒";
            protectionStatusLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            protectionStatusLabel.ForeColor = Color.FromArgb(0, 200, 100);
            protectionStatusLabel.Size = new Size(800, 40);
            protectionStatusLabel.Location = new Point(50, 450);
            protectionStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            protectionStatusLabel.BackColor = Color.FromArgb(0, 0, 0, 0);
            reviewCard.Controls.Add(protectionStatusLabel);

            // Анимированное мерцание статуса
            Timer blinkTimer = new Timer();
            blinkTimer.Interval = 800;
            blinkTimer.Tick += (s, e) =>
            {
                protectionStatusLabel.ForeColor = protectionStatusLabel.ForeColor == Color.FromArgb(0, 200, 100)
                    ? Color.Gold
                    : Color.FromArgb(0, 200, 100);
            };
            blinkTimer.Start();
        }

        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl;
            using (Brush backBrush = new SolidBrush(Color.FromArgb(0, 80, 200)))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }
            using (Brush textBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(tc.TabPages[e.Index].Text, e.Font, textBrush, e.Bounds, sf);
            }
        }

        private Panel CreateMovieCard(MovieData movie)
        {
            Panel card = new Panel();
            card.Size = new Size(280, 400);
            card.BackColor = Color.FromArgb(45, 45, 55);
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(15);
            card.Padding = new Padding(5);

            Panel posterPanel = new Panel();
            posterPanel.Size = new Size(250, 200);
            posterPanel.Location = new Point(10, 10);
            posterPanel.BackColor = ColorTranslator.FromHtml(movie.ColorHex);
            posterPanel.BorderStyle = BorderStyle.FixedSingle;
            card.Controls.Add(posterPanel);

            Label posterText = new Label();
            posterText.Text = movie.Title;
            posterText.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            posterText.ForeColor = Color.White;
            posterText.Size = new Size(250, 50);
            posterText.Location = new Point(0, 75);
            posterText.TextAlign = ContentAlignment.MiddleCenter;
            posterPanel.Controls.Add(posterText);

            Label titleLabel = new Label();
            titleLabel.Text = movie.Title;
            titleLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(100, 150, 255);
            titleLabel.Location = new Point(10, 225);
            titleLabel.Size = new Size(250, 30);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(titleLabel);

            Label genreLabel = new Label();
            genreLabel.Text = $"{movie.Genre} | {movie.Duration} мин";
            genreLabel.Font = new Font("Segoe UI", 10);
            genreLabel.ForeColor = Color.LightGray;
            genreLabel.Location = new Point(10, 255);
            genreLabel.Size = new Size(250, 25);
            card.Controls.Add(genreLabel);

            Label releaseLabel = new Label();
            releaseLabel.Text = $"Прокат: {movie.ReleaseStart} - {movie.ReleaseEnd}";
            releaseLabel.Font = new Font("Segoe UI", 9);
            releaseLabel.ForeColor = Color.FromArgb(100, 150, 255);
            releaseLabel.Location = new Point(10, 280);
            releaseLabel.Size = new Size(250, 20);
            card.Controls.Add(releaseLabel);

            Label priceLabel = new Label();
            priceLabel.Text = $"{movie.Price} ₽";
            priceLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            priceLabel.ForeColor = Color.FromArgb(0, 200, 100);
            priceLabel.Location = new Point(10, 305);
            priceLabel.Size = new Size(250, 35);
            priceLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(priceLabel);

            Button bookBtn = new Button();
            bookBtn.Text = "ЗАБРОНИРОВАТЬ";
            bookBtn.Size = new Size(250, 38);
            bookBtn.Location = new Point(10, 350);
            bookBtn.BackColor = Color.FromArgb(0, 80, 200);
            bookBtn.ForeColor = Color.White;
            bookBtn.FlatStyle = FlatStyle.Flat;
            bookBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            bookBtn.Click += (s, e) =>
            {
                BookingForm bookingForm = new BookingForm(movie.Id, movie.Title, movie.Price, connectionString);
                bookingForm.ShowDialog();
                LoadTickets();
            };
            card.Controls.Add(bookBtn);

            return card;
        }

        private void LoadRegularClients()
        {
            if (clientsPanel == null) return;
            clientsPanel.Controls.Clear();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT name, last_visit, visit_count FROM regular_clients ORDER BY visit_count DESC", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0);
                        string lastVisit = reader.GetString(1);
                        int visitCount = reader.GetInt32(2);

                        Panel clientCard = new Panel();
                        clientCard.Size = new Size(300, 150);
                        clientCard.BackColor = Color.FromArgb(45, 45, 55);
                        clientCard.BorderStyle = BorderStyle.FixedSingle;
                        clientCard.Margin = new Padding(15);

                        Label nameLabel = new Label();
                        nameLabel.Text = $"👤 {name}";
                        nameLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
                        nameLabel.ForeColor = Color.FromArgb(100, 150, 255);
                        nameLabel.Size = new Size(260, 35);
                        nameLabel.Location = new Point(15, 15);
                        clientCard.Controls.Add(nameLabel);

                        Label lastVisitLabel = new Label();
                        lastVisitLabel.Text = $"📅 Последний визит: {lastVisit}";
                        lastVisitLabel.Font = new Font("Segoe UI", 10);
                        lastVisitLabel.ForeColor = Color.LightGray;
                        lastVisitLabel.Size = new Size(260, 30);
                        lastVisitLabel.Location = new Point(15, 55);
                        clientCard.Controls.Add(lastVisitLabel);

                        Label visitCountLabel = new Label();
                        visitCountLabel.Text = $"🎫 Количество посещений: {visitCount}";
                        visitCountLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                        visitCountLabel.ForeColor = Color.FromArgb(0, 200, 100);
                        visitCountLabel.Size = new Size(260, 30);
                        visitCountLabel.Location = new Point(15, 90);
                        clientCard.Controls.Add(visitCountLabel);

                        clientsPanel.Controls.Add(clientCard);
                    }
                }
            }
        }

        private void LoadNews()
        {
            if (newsPanel == null) return;
            newsPanel.Controls.Clear();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT id, title, content, publish_date FROM news ORDER BY publish_date DESC", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string title = reader.GetString(1);
                        string content = reader.GetString(2);
                        string date = reader.GetString(3);

                        Panel newsCard = new Panel();
                        newsCard.Size = new Size(340, 250);
                        newsCard.BackColor = Color.FromArgb(45, 45, 55);
                        newsCard.BorderStyle = BorderStyle.FixedSingle;
                        newsCard.Margin = new Padding(15);

                        Label emojiLabel = new Label();
                        emojiLabel.Text = "📰";
                        emojiLabel.Font = new Font("Segoe UI", 48, FontStyle.Bold);
                        emojiLabel.ForeColor = Color.Gold;
                        emojiLabel.Size = new Size(310, 70);
                        emojiLabel.Location = new Point(15, 10);
                        emojiLabel.TextAlign = ContentAlignment.MiddleCenter;
                        newsCard.Controls.Add(emojiLabel);

                        Label dateLabel = new Label();
                        dateLabel.Text = date;
                        dateLabel.Font = new Font("Segoe UI", 9);
                        dateLabel.ForeColor = Color.Gray;
                        dateLabel.Size = new Size(310, 20);
                        dateLabel.Location = new Point(15, 90);
                        newsCard.Controls.Add(dateLabel);

                        Label titleLabel = new Label();
                        titleLabel.Text = title;
                        titleLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                        titleLabel.ForeColor = Color.FromArgb(100, 150, 255);
                        titleLabel.Size = new Size(310, 30);
                        titleLabel.Location = new Point(15, 115);
                        newsCard.Controls.Add(titleLabel);

                        string shortContent = content.Length > 80 ? content.Substring(0, 77) + "..." : content;
                        Label contentLabel = new Label();
                        contentLabel.Text = shortContent;
                        contentLabel.Font = new Font("Segoe UI", 9);
                        contentLabel.ForeColor = Color.LightGray;
                        contentLabel.Size = new Size(310, 60);
                        contentLabel.Location = new Point(15, 150);
                        newsCard.Controls.Add(contentLabel);

                        newsPanel.Controls.Add(newsCard);
                    }
                }
            }
        }

        private void LoadMoviesInRandomOrder()
        {
            moviesPanel.Controls.Clear();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT id, title, genre, duration, poster_color, description, price, release_start, release_end FROM movies", conn);
                var movies = new List<MovieData>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        movies.Add(new MovieData
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Genre = reader.GetString(2),
                            Duration = reader.GetInt32(3),
                            ColorHex = reader.GetString(4),
                            Description = reader.GetString(5),
                            Price = reader.GetDouble(6),
                            ReleaseStart = reader.GetString(7),
                            ReleaseEnd = reader.GetString(8)
                        });
                    }
                }
                movies = movies.OrderBy(x => random.Next()).ToList();
                foreach (var movie in movies)
                {
                    Panel card = CreateMovieCard(movie);
                    moviesPanel.Controls.Add(card);
                }
            }
        }

        private void LoadEmployees()
        {
            if (employeesPanel == null) return;
            employeesPanel.Controls.Clear();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT id, name, position, phone, salary, hire_date FROM employees", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(1);
                        string position = reader.GetString(2);
                        string phone = reader.GetString(3);
                        double salary = reader.GetDouble(4);
                        string hireDate = reader.GetString(5);

                        Panel empCard = new Panel();
                        empCard.Size = new Size(280, 180);
                        empCard.BackColor = Color.FromArgb(45, 45, 55);
                        empCard.BorderStyle = BorderStyle.FixedSingle;
                        empCard.Margin = new Padding(12);

                        Panel avatarPanel = new Panel();
                        avatarPanel.Size = new Size(60, 60);
                        avatarPanel.Location = new Point(15, 15);
                        avatarPanel.BackColor = Color.FromArgb(0, 80, 200);
                        avatarPanel.BorderStyle = BorderStyle.FixedSingle;
                        empCard.Controls.Add(avatarPanel);

                        Label avatarText = new Label();
                        avatarText.Text = name.Length > 0 ? name[0].ToString() : "?";
                        avatarText.Font = new Font("Segoe UI", 24, FontStyle.Bold);
                        avatarText.ForeColor = Color.White;
                        avatarText.Size = new Size(60, 60);
                        avatarText.TextAlign = ContentAlignment.MiddleCenter;
                        avatarPanel.Controls.Add(avatarText);

                        Label nameLabel = new Label();
                        nameLabel.Text = name;
                        nameLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                        nameLabel.ForeColor = Color.FromArgb(100, 150, 255);
                        nameLabel.Location = new Point(90, 15);
                        nameLabel.Size = new Size(175, 25);
                        empCard.Controls.Add(nameLabel);

                        Label positionLabel = new Label();
                        positionLabel.Text = $"📌 {position}";
                        positionLabel.Font = new Font("Segoe UI", 10);
                        positionLabel.ForeColor = Color.LightGray;
                        positionLabel.Location = new Point(90, 45);
                        positionLabel.Size = new Size(175, 25);
                        empCard.Controls.Add(positionLabel);

                        Label phoneLabel = new Label();
                        phoneLabel.Text = $"📞 {phone}";
                        phoneLabel.Font = new Font("Segoe UI", 9);
                        phoneLabel.ForeColor = Color.Gray;
                        phoneLabel.Location = new Point(15, 90);
                        phoneLabel.Size = new Size(250, 20);
                        empCard.Controls.Add(phoneLabel);

                        Label salaryLabel = new Label();
                        salaryLabel.Text = $"💰 {salary} ₽";
                        salaryLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                        salaryLabel.ForeColor = Color.FromArgb(0, 200, 100);
                        salaryLabel.Location = new Point(15, 115);
                        salaryLabel.Size = new Size(250, 25);
                        empCard.Controls.Add(salaryLabel);

                        Label dateLabel = new Label();
                        dateLabel.Text = $"📅 с {hireDate}";
                        dateLabel.Font = new Font("Segoe UI", 9);
                        dateLabel.ForeColor = Color.Gray;
                        dateLabel.Location = new Point(15, 145);
                        dateLabel.Size = new Size(250, 20);
                        empCard.Controls.Add(dateLabel);

                        employeesPanel.Controls.Add(empCard);
                    }
                }
            }
        }

        private void LoadTickets()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT id, movie_title as 'Фильм', row_num as 'Ряд', seat_num as 'Место', price as 'Цена', booking_date as 'Дата', ticket_code as 'Код' FROM tickets ORDER BY movie_title, booking_date DESC";
                var adapter = new SQLiteDataAdapter(query, conn);
                var dt = new System.Data.DataTable();
                adapter.Fill(dt);

                HashSet<int> selectedIds = new HashSet<int>();
                if (ticketsGridView.Rows.Count > 0 && ticketsGridView.Columns.Contains("Select"))
                {
                    foreach (DataGridViewRow row in ticketsGridView.Rows)
                    {
                        if (!row.IsNewRow && row.Cells["Select"] is DataGridViewCheckBoxCell checkCell &&
                            checkCell.Value != null &&
                            Convert.ToBoolean(checkCell.Value))
                        {
                            int id = Convert.ToInt32(row.Cells["id"].Value);
                            selectedIds.Add(id);
                        }
                    }
                }

                ticketsGridView.DataSource = dt;

                if (!ticketsGridView.Columns.Contains("Select"))
                {
                    DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
                    checkColumn.Name = "Select";
                    checkColumn.HeaderText = "✓";
                    checkColumn.Width = 40;
                    checkColumn.ReadOnly = false;
                    ticketsGridView.Columns.Insert(0, checkColumn);
                }

                foreach (DataGridViewColumn col in ticketsGridView.Columns)
                {
                    if (col.Name != "Select")
                    {
                        col.ReadOnly = true;
                    }
                }

                if (selectedIds.Count > 0)
                {
                    foreach (DataGridViewRow row in ticketsGridView.Rows)
                    {
                        if (!row.IsNewRow && row.Cells["id"].Value != null)
                        {
                            int id = Convert.ToInt32(row.Cells["id"].Value);
                            if (selectedIds.Contains(id))
                            {
                                row.Cells["Select"].Value = true;
                            }
                        }
                    }
                }

                if (ticketsGridView.Columns.Contains("id"))
                    ticketsGridView.Columns["id"].Visible = false;

                UpdateTotalSum();
            }
        }

        private void UpdateTotalSum()
        {
            double total = 0;
            if (ticketsGridView.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in ticketsGridView.Rows)
                {
                    if (row.Cells["Цена"].Value != null)
                        total += Convert.ToDouble(row.Cells["Цена"].Value);
                }
            }
            totalSumLabel.Text = $"💰 Общая сумма: {total} ₽";
        }

        private void DeleteSelectedTickets(object sender, EventArgs e)
        {
            List<int> selectedIds = new List<int>();

            foreach (DataGridViewRow row in ticketsGridView.Rows)
            {
                if (!row.IsNewRow && row.Cells["Select"].Value != null &&
                    row.Cells["Select"] is DataGridViewCheckBoxCell &&
                    Convert.ToBoolean(row.Cells["Select"].Value) == true)
                {
                    int id = Convert.ToInt32(row.Cells["id"].Value);
                    selectedIds.Add(id);
                }
            }

            if (selectedIds.Count == 0)
            {
                MessageBox.Show("Выберите билеты для удаления (отметьте их чекбоксами)!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить выбранные {selectedIds.Count} билет(ов)?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (int id in selectedIds)
                            {
                                var ticket = new SQLiteCommand($"SELECT movie_id, row_num, seat_num FROM tickets WHERE id = {id}", conn);
                                using (var reader = ticket.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        int movieId = reader.GetInt32(0);
                                        int rowNum = reader.GetInt32(1);
                                        int seatNum = reader.GetInt32(2);
                                        new SQLiteCommand($"UPDATE seats SET is_free=1 WHERE movie_id={movieId} AND row_num={rowNum} AND seat_num={seatNum}", conn).ExecuteNonQuery();
                                    }
                                }
                                new SQLiteCommand($"DELETE FROM tickets WHERE id = {id}", conn).ExecuteNonQuery();
                            }
                            transaction.Commit();
                            LoadTickets();
                            MessageBox.Show($"Удалено {selectedIds.Count} билет(ов)!", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private class MovieData
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Genre { get; set; }
            public int Duration { get; set; }
            public string ColorHex { get; set; }
            public string Description { get; set; }
            public double Price { get; set; }
            public string ReleaseStart { get; set; }
            public string ReleaseEnd { get; set; }
        }
    }

    // ==================== ФОРМА БРОНИРОВАНИЯ ====================
    public class BookingForm : Form
    {
        private int movieId;
        private string movieTitle;
        private double moviePrice;
        private string connectionString;
        private Button[,] seatButtons = new Button[6, 8];
        private ListBox cartListBox;
        private Label totalLabel;

        public BookingForm(int id, string title, double price, string connString)
        {
            movieId = id;
            movieTitle = title;
            moviePrice = price;
            connectionString = connString;
            this.Text = $"Бронирование: {movieTitle}";
            this.Size = new Size(1350, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 40);
            CreateInterface();
            LoadSeatsFromDB();
        }

        private void LoadSeatsFromDB()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var cmd = new SQLiteCommand($"SELECT is_free FROM seats WHERE movie_id={movieId} AND row_num={i + 1} AND seat_num={j + 1}", conn);
                        object result = cmd.ExecuteScalar();
                        bool isFree = (result != null && Convert.ToInt32(result) == 1);
                        seatButtons[i, j].BackColor = isFree ? Color.FromArgb(0, 150, 0) : Color.FromArgb(200, 50, 50);
                    }
                }
            }
        }

        private void CreateInterface()
        {
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 90;
            topPanel.BackColor = Color.FromArgb(0, 60, 140);
            this.Controls.Add(topPanel);

            Button closeBtn = new Button();
            closeBtn.Text = "✖";
            closeBtn.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            closeBtn.Size = new Size(40, 40);
            closeBtn.Location = new Point(this.Width - 55, 25);
            closeBtn.BackColor = Color.FromArgb(200, 50, 50);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Click += (s, e) => this.Close();
            topPanel.Controls.Add(closeBtn);

            Label titleLabel = new Label();
            titleLabel.Text = movieTitle;
            titleLabel.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(25, 20);
            titleLabel.Size = new Size(550, 45);
            topPanel.Controls.Add(titleLabel);

            Label priceLabel = new Label();
            priceLabel.Text = $"💰 Цена билета: {moviePrice} ₽";
            priceLabel.Font = new Font("Segoe UI", 13);
            priceLabel.ForeColor = Color.FromArgb(255, 215, 0);
            priceLabel.Location = new Point(25, 65);
            priceLabel.Size = new Size(300, 30);
            topPanel.Controls.Add(priceLabel);

            Label screenLabel = new Label();
            screenLabel.Text = "🎬 ЭКРАН 🎬";
            screenLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            screenLabel.ForeColor = Color.White;
            screenLabel.BackColor = Color.Black;
            screenLabel.Size = new Size(500, 45);
            screenLabel.Location = new Point(350, 110);
            screenLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(screenLabel);

            int startX = 200, startY = 180, stepX = 58, stepY = 52;

            for (int i = 0; i < 6; i++)
            {
                Label rowLabel = new Label();
                rowLabel.Text = $"Ряд {i + 1}";
                rowLabel.ForeColor = Color.FromArgb(100, 150, 255);
                rowLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                rowLabel.Location = new Point(110, startY + i * stepY);
                rowLabel.Size = new Size(70, 42);
                rowLabel.TextAlign = ContentAlignment.MiddleRight;
                this.Controls.Add(rowLabel);

                for (int j = 0; j < 8; j++)
                {
                    Button btn = new Button();
                    btn.Text = $"{i + 1}-{j + 1}";
                    btn.Size = new Size(52, 44);
                    btn.Location = new Point(startX + j * stepX, startY + i * stepY);
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    btn.BackColor = Color.FromArgb(0, 150, 0);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Cursor = Cursors.Hand;

                    int row = i, col = j;
                    btn.Click += (s, e) => SeatButton_Click(btn, row, col);
                    seatButtons[i, j] = btn;
                    this.Controls.Add(btn);
                }
            }

            Panel cartPanel = new Panel();
            cartPanel.Size = new Size(380, 470);
            cartPanel.Location = new Point(880, 110);
            cartPanel.BackColor = Color.FromArgb(45, 45, 55);
            cartPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(cartPanel);

            Label cartLabel = new Label();
            cartLabel.Text = "🛒 КОРЗИНА";
            cartLabel.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            cartLabel.ForeColor = Color.FromArgb(100, 150, 255);
            cartLabel.Location = new Point(15, 15);
            cartLabel.Size = new Size(200, 40);
            cartPanel.Controls.Add(cartLabel);

            cartListBox = new ListBox();
            cartListBox.Size = new Size(345, 260);
            cartListBox.Location = new Point(15, 65);
            cartListBox.BackColor = Color.FromArgb(35, 35, 45);
            cartListBox.ForeColor = Color.White;
            cartListBox.Font = new Font("Segoe UI", 10);
            cartListBox.BorderStyle = BorderStyle.FixedSingle;
            cartPanel.Controls.Add(cartListBox);

            totalLabel = new Label();
            totalLabel.Text = "Итого: 0 ₽";
            totalLabel.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            totalLabel.ForeColor = Color.FromArgb(0, 200, 100);
            totalLabel.Location = new Point(15, 345);
            totalLabel.Size = new Size(345, 45);
            totalLabel.TextAlign = ContentAlignment.MiddleCenter;
            cartPanel.Controls.Add(totalLabel);

            Button purchaseBtn = new Button();
            purchaseBtn.Text = "💳 КУПИТЬ БИЛЕТЫ 💳";
            purchaseBtn.Size = new Size(345, 60);
            purchaseBtn.Location = new Point(15, 400);
            purchaseBtn.BackColor = Color.FromArgb(0, 120, 0);
            purchaseBtn.ForeColor = Color.White;
            purchaseBtn.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            purchaseBtn.FlatStyle = FlatStyle.Flat;
            purchaseBtn.Cursor = Cursors.Hand;
            purchaseBtn.Click += PurchaseTickets;
            cartPanel.Controls.Add(purchaseBtn);

            Panel legendPanel = new Panel();
            legendPanel.Size = new Size(450, 50);
            legendPanel.Location = new Point(350, 620);
            legendPanel.BackColor = Color.FromArgb(45, 45, 55);
            this.Controls.Add(legendPanel);

            Label legend = new Label();
            legend.Text = "🟢 Свободно     🟡 Выбрано     🔴 Занято";
            legend.ForeColor = Color.White;
            legend.Font = new Font("Segoe UI", 11);
            legend.Location = new Point(15, 12);
            legend.Size = new Size(420, 30);
            legendPanel.Controls.Add(legend);
        }

        private void SeatButton_Click(Button btn, int row, int col)
        {
            if (btn.BackColor == Color.FromArgb(200, 50, 50))
            {
                MessageBox.Show("❌ Это место уже занято!");
                return;
            }

            if (btn.BackColor == Color.FromArgb(0, 150, 0))
            {
                btn.BackColor = Color.FromArgb(255, 200, 0);
                cartListBox.Items.Add($"Ряд {row + 1}, Место {col + 1} - {moviePrice} ₽");
            }
            else if (btn.BackColor == Color.FromArgb(255, 200, 0))
            {
                btn.BackColor = Color.FromArgb(0, 150, 0);
                string itemToRemove = $"Ряд {row + 1}, Место {col + 1} - {moviePrice} ₽";
                if (cartListBox.Items.Contains(itemToRemove))
                    cartListBox.Items.Remove(itemToRemove);
            }
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            totalLabel.Text = $"Итого: {cartListBox.Items.Count * moviePrice} ₽";
        }

        private void PurchaseTickets(object sender, EventArgs e)
        {
            if (cartListBox.Items.Count == 0)
            {
                MessageBox.Show("Выберите места!");
                return;
            }

            string tickets = "";
            string ticketCodes = "";
            Random rnd = new Random();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        Button btn = seatButtons[i, j];
                        if (btn.BackColor == Color.FromArgb(255, 200, 0))
                        {
                            string ticketCode = $"TKT-{rnd.Next(1000, 9999)}-{DateTime.Now.Ticks % 10000}";
                            ticketCodes += $"{ticketCode}\n";
                            tickets += $"Ряд {i + 1}, Место {j + 1}\n";
                            btn.BackColor = Color.FromArgb(200, 50, 50);

                            new SQLiteCommand($"UPDATE seats SET is_free=0 WHERE movie_id={movieId} AND row_num={i + 1} AND seat_num={j + 1}", conn).ExecuteNonQuery();

                            var insert = new SQLiteCommand("INSERT INTO tickets (movie_id, movie_title, row_num, seat_num, price, booking_date, ticket_code) VALUES (@mid, @mt, @row, @seat, @price, @date, @code)", conn);
                            insert.Parameters.AddWithValue("@mid", movieId);
                            insert.Parameters.AddWithValue("@mt", movieTitle);
                            insert.Parameters.AddWithValue("@row", i + 1);
                            insert.Parameters.AddWithValue("@seat", j + 1);
                            insert.Parameters.AddWithValue("@price", moviePrice);
                            insert.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            insert.Parameters.AddWithValue("@code", ticketCode);
                            insert.ExecuteNonQuery();
                        }
                    }
                }
            }

            ShowSuccessDialog(movieTitle, cartListBox.Items.Count, cartListBox.Items.Count * moviePrice, tickets, ticketCodes);

            cartListBox.Items.Clear();
            UpdateTotal();
        }

        private void ShowSuccessDialog(string movieTitle, int count, double total, string tickets, string codes)
        {
            Form successForm = new Form();
            successForm.Text = "✅ УСПЕХ!";
            successForm.Size = new Size(550, 550);
            successForm.StartPosition = FormStartPosition.CenterParent;
            successForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            successForm.MaximizeBox = false;
            successForm.BackColor = Color.FromArgb(30, 30, 40);
            successForm.Paint += (s, pe) =>
            {
                Rectangle rect = successForm.ClientRectangle;
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(0, 80, 200), Color.FromArgb(0, 50, 150), 90f))
                {
                    pe.Graphics.FillRectangle(brush, rect);
                }
            };

            Label checkLabel = new Label();
            checkLabel.Text = "✅";
            checkLabel.Font = new Font("Segoe UI Emoji", 72, FontStyle.Bold);
            checkLabel.ForeColor = Color.FromArgb(0, 200, 100);
            checkLabel.Size = new Size(100, 100);
            checkLabel.Location = new Point(210, 20);
            checkLabel.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(checkLabel);

            Label titleLabel = new Label();
            titleLabel.Text = "БИЛЕТЫ УСПЕШНО КУПЛЕНЫ!";
            titleLabel.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Size = new Size(500, 40);
            titleLabel.Location = new Point(20, 130);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(titleLabel);

            Label movieInfo = new Label();
            movieInfo.Text = $"🎬 {movieTitle}";
            movieInfo.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            movieInfo.ForeColor = Color.FromArgb(100, 150, 255);
            movieInfo.Size = new Size(480, 35);
            movieInfo.Location = new Point(20, 180);
            movieInfo.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(movieInfo);

            Label countLabel = new Label();
            countLabel.Text = $"🎫 Количество билетов: {count}";
            countLabel.Font = new Font("Segoe UI", 12);
            countLabel.ForeColor = Color.White;
            countLabel.Size = new Size(480, 30);
            countLabel.Location = new Point(20, 220);
            countLabel.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(countLabel);

            Label totalLabel2 = new Label();
            totalLabel2.Text = $"💰 Общая сумма: {total} ₽";
            totalLabel2.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            totalLabel2.ForeColor = Color.FromArgb(0, 200, 100);
            totalLabel2.Size = new Size(480, 35);
            totalLabel2.Location = new Point(20, 255);
            totalLabel2.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(totalLabel2);

            Panel separator = new Panel();
            separator.Size = new Size(450, 2);
            separator.Location = new Point(40, 305);
            separator.BackColor = Color.Gold;
            successForm.Controls.Add(separator);

            Label seatsHeader = new Label();
            seatsHeader.Text = "📌 ВЫБРАННЫЕ МЕСТА:";
            seatsHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            seatsHeader.ForeColor = Color.Gold;
            seatsHeader.Size = new Size(480, 30);
            seatsHeader.Location = new Point(20, 320);
            seatsHeader.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(seatsHeader);

            TextBox seatsBox = new TextBox();
            seatsBox.Text = tickets;
            seatsBox.Font = new Font("Segoe UI", 10);
            seatsBox.Multiline = true;
            seatsBox.Size = new Size(460, 70);
            seatsBox.Location = new Point(35, 355);
            seatsBox.BackColor = Color.FromArgb(45, 45, 55);
            seatsBox.ForeColor = Color.White;
            seatsBox.BorderStyle = BorderStyle.FixedSingle;
            seatsBox.ReadOnly = true;
            successForm.Controls.Add(seatsBox);

            Label codesHeader = new Label();
            codesHeader.Text = "🔑 КОДЫ БИЛЕТОВ (сохраните для входа):";
            codesHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            codesHeader.ForeColor = Color.Gold;
            codesHeader.Size = new Size(480, 30);
            codesHeader.Location = new Point(20, 435);
            codesHeader.TextAlign = ContentAlignment.MiddleCenter;
            successForm.Controls.Add(codesHeader);

            TextBox codesBox = new TextBox();
            codesBox.Text = codes;
            codesBox.Font = new Font("Courier New", 10, FontStyle.Bold);
            codesBox.Multiline = true;
            codesBox.Size = new Size(460, 60);
            codesBox.Location = new Point(35, 468);
            codesBox.BackColor = Color.FromArgb(45, 45, 55);
            codesBox.ForeColor = Color.FromArgb(0, 200, 100);
            codesBox.BorderStyle = BorderStyle.FixedSingle;
            codesBox.ReadOnly = true;
            successForm.Controls.Add(codesBox);

            Button okBtn = new Button();
            okBtn.Text = "ОТЛИЧНО!";
            okBtn.Size = new Size(150, 45);
            okBtn.Location = new Point(185, 540);
            okBtn.BackColor = Color.FromArgb(0, 120, 0);
            okBtn.ForeColor = Color.White;
            okBtn.FlatStyle = FlatStyle.Flat;
            okBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            okBtn.Cursor = Cursors.Hand;
            okBtn.Click += (s, ev) => successForm.Close();
            successForm.Controls.Add(okBtn);

            successForm.ShowDialog();
        }
    }
}
