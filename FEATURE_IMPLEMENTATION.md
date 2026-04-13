# Beauty E-commerce Feature Implementation

## Overview
This document describes the implementation of three key features designed to increase conversion, retention, and repeat purchases for the beauty e-commerce platform.

---

## 1. Skin Consultation Quiz (Tăng Conversion)

### Purpose
Help customers find personalized product recommendations through an interactive skin analysis quiz, increasing conversion rates by providing tailored suggestions.

### Domain Entities Created

#### `SkinProfile`
- Stores user's skin type, concerns, and characteristics
- Tracks sensitivity level, acne, dark spots, wrinkles, pores, dehydration
- Records environmental and lifestyle factors
- Calculates overall skin health score

#### `QuizQuestion` & `QuizAnswer`
- Configurable quiz questions with categories (Skin Type, Concerns, Lifestyle)
- Multiple question types: SingleChoice, MultipleChoice, Scale
- Answer options tagged with skin type and concern metadata for analysis

#### `QuizResult`
- Tracks user's quiz progress and completion
- Stores detailed analysis results in JSON format
- Recommends skin type and top concerns based on answers

#### `QuizAnswerLog`
- Logs each answer for analytics and improvement
- Tracks time spent on questions

#### `ProductRecommendation`
- Links skin profiles to recommended products
- Includes match score and reason for recommendation
- Priority-based ordering for display

### Frontend Components (To be implemented)
- Interactive quiz UI with progress indicator
- Visual skin type selector with images
- Results page with personalized product recommendations
- "Add all to cart" convenience feature

### API Endpoints (To be implemented)
- `GET /api/quiz/questions` - Get quiz questions
- `POST /api/quiz/start` - Start a new quiz session
- `POST /api/quiz/answer` - Submit an answer
- `GET /api/quiz/results/{quizId}` - Get quiz results and recommendations
- `GET /api/skin-profile` - Get user's current skin profile
- `PUT /api/skin-profile` - Update skin profile

### Business Value
- **Conversion Lift**: Personalized recommendations increase purchase likelihood
- **Data Collection**: Gather valuable customer skin data for marketing
- **Engagement**: Interactive experience keeps users on site longer

---

## 2. Routine Tracker with Reminders (Tăng Retention)

### Purpose
Help users build consistent skincare habits by tracking their daily routine and sending timely reminders, increasing app retention and engagement.

### Domain Entities Created

#### `SkincareRoutine`
- User's personalized routine (Morning, Evening, or Both)
- Tracks streak days and total completed days for gamification
- Multiple routines per user supported

#### `RoutineStep`
- Individual steps in order (Cleanser → Toner → Serum → Moisturizer)
- Links to specific products or custom steps
- Includes instructions and estimated duration
- Supports multiple reminders per step

#### `RoutineLog` & `StepLog`
- Daily tracking of routine completion
- Records mood and skin condition notes
- Tracks actual vs estimated duration

#### `Reminder`
- Configurable reminder times and days of week
- Multiple notification channels: Push, Email, SMS
- Custom reminder text per step

### Frontend Components (To be implemented)
- Routine builder with drag-and-drop step ordering
- Daily checklist with timer functionality
- Streak counter and achievement badges
- Reminder settings page
- Progress charts and skin journey timeline

### API Endpoints (To be implemented)
- `GET /api/routines` - Get user's routines
- `POST /api/routines` - Create new routine
- `PUT /api/routines/{id}` - Update routine
- `POST /api/routines/{id}/complete` - Mark routine as complete
- `GET /api/routines/{id}/logs` - Get routine history
- `POST /api/reminders` - Set up reminders
- `PUT /api/reminders/{id}` - Update/disable reminders

### Notification Service Integration
- Background job to check and send due reminders
- Integration with existing `IEmailSender` and `ISmsSender`
- Push notification support via Firebase/APNS

### Business Value
- **Retention**: Daily engagement builds habit
- **Product Usage**: Ensures customers use purchased products effectively
- **Data Insights**: Understand usage patterns for better recommendations

---

## 3. Expiry Alerts & Auto-Replenish (Tăng Repeat Purchases)

### Purpose
Track product expiry dates and automatically remind users to repurchase, reducing churn and increasing customer lifetime value.

### Domain Entities Created

#### `UserProduct`
- Tracks products owned by user from purchase history
- Records opening date and calculates expiry (PAO - Period After Opening)
- Monitors quantity and usage patterns
- Configurable alert preferences

#### `UsageLog`
- Daily product usage tracking
- Records quantity used and application area
- Optional skin reaction rating

#### `ExpiryAlert`
- Automated alerts for expiring products
- Low stock warnings
- Replenishment reminders
- Tracks alert status and user actions

#### `ReplenishmentSubscription`
- Subscription-based auto-replenishment
- Configurable frequency and quantity
- Subscriber discounts
- Pause/resume/cancel flexibility

#### `ReplenishmentOrder`
- Individual delivery instances from subscription
- Full order lifecycle tracking
- Links to main Order system

### Frontend Components (To be implemented)
- "My Products" inventory dashboard
- Expiry timeline visualization
- One-click replenishment setup
- Subscription management page
- Usage tracking quick-logs

### API Endpoints (To be implemented)
- `GET /api/my-products` - Get user's product inventory
- `POST /api/my-products` - Add product manually
- `PUT /api/my-products/{id}/usage` - Log product usage
- `GET /api/expiry-alerts` - Get upcoming expiry alerts
- `POST /api/replenishment-subscriptions` - Create subscription
- `GET /api/replenishment-subscriptions` - Get active subscriptions
- `PUT /api/replenishment-subscriptions/{id}/pause` - Pause subscription
- `DELETE /api/replenishment-subscriptions/{id}` - Cancel subscription

### Background Services (To be implemented)
- Daily job to check expiring products
- Automatic alert generation
- Subscription order processing
- Inventory depletion calculations

### Business Value
- **Repeat Purchases**: Timely reminders prevent customers from running out
- **Subscription Revenue**: Recurring revenue from auto-replenishment
- **Customer Loyalty**: Helpful service builds trust and retention
- **Reduced Waste**: Helps customers use products before expiry

---

## Database Migrations Required

The following tables need to be created:

### Skin Consultation
- `SkinProfiles`
- `QuizQuestions`
- `QuizAnswers`
- `QuizResults`
- `QuizAnswerLogs`
- `ProductRecommendations`

### Routine Tracker
- `SkincareRoutines`
- `RoutineSteps`
- `RoutineLogs`
- `StepLogs`
- `Reminders`

### Expiry & Replenishment
- `UserProducts`
- `UsageLogs`
- `ExpiryAlerts`
- `ReplenishmentSubscriptions`
- `ReplenishmentOrders`

---

## Integration Points

### With Existing Systems

1. **Product Catalog**
   - Product recommendations link to existing `Product` entities
   - Routine steps can reference products
   - Replenishment subscriptions tied to products

2. **User Management**
   - All features tied to existing `User` entity
   - Leverage existing authentication and authorization

3. **Order System**
   - `UserProduct` links to `OrderItem` for purchase history
   - `ReplenishmentOrder` creates actual `Order` records

4. **Notification System**
   - Uses existing `IEmailSender` and `ISmsSender` interfaces
   - Extends with push notification support

5. **Payment System**
   - Replenishment subscriptions use existing payment methods
   - Recurring billing integration required

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
- [ ] Database migrations for all entities
- [ ] Repository implementations
- [ ] Basic CRUD API endpoints
- [ ] Entity validation and business logic

### Phase 2: Skin Quiz (Week 3-4)
- [ ] Quiz question seeding (default questions)
- [ ] Quiz completion flow
- [ ] Recommendation algorithm
- [ ] Frontend quiz UI
- [ ] Results page with product carousel

### Phase 3: Routine Tracker (Week 5-6)
- [ ] Routine builder UI
- [ ] Daily tracking interface
- [ ] Reminder background service
- [ ] Push notification integration
- [ ] Streak and achievement system

### Phase 4: Expiry & Replenishment (Week 7-8)
- [ ] Product import from order history
- [ ] Expiry calculation logic
- [ ] Alert generation service
- [ ] Subscription management UI
- [ ] Recurring order processing
- [ ] Payment integration for subscriptions

### Phase 5: Analytics & Optimization (Week 9-10)
- [ ] Dashboard for feature usage metrics
- [ ] A/B testing framework
- [ ] Conversion funnel analysis
- [ ] Algorithm refinement based on data

---

## Success Metrics

### Skin Quiz
- Quiz completion rate
- Conversion rate: Quiz takers vs non-takers
- Average order value from quiz recommendations

### Routine Tracker
- Daily active users (DAU)
- Routine completion rate
- 7-day and 30-day retention rates
- Reminder engagement rate

### Expiry & Replenishment
- Repeat purchase rate
- Subscription signup rate
- Churn reduction
- Customer lifetime value (CLV) increase

---

## Technical Considerations

### Security
- All user data encrypted at rest
- Secure handling of health-related skin data
- PCI compliance for subscription payments

### Performance
- Caching for quiz questions and recommendations
- Efficient querying for routine logs
- Batch processing for daily alerts

### Scalability
- Horizontal scaling for notification services
- Database indexing on frequently queried fields
- CDN for quiz images and assets

### Compliance
- GDPR compliance for personal data
- Clear privacy policy for skin data usage
- Easy data export and deletion

---

## Next Steps

1. Review and approve entity designs
2. Create database migration scripts
3. Implement repository layer
4. Build API endpoints
5. Develop frontend components
6. Integrate notification services
7. Test end-to-end flows
8. Deploy to staging for UAT
9. Launch with monitoring dashboards
